mod macos;
mod windows;

#[cfg(target_os = "macos")]
use macos as platform;
#[cfg(target_os = "windows")]
use windows as platform;

use crate::engine::ProxyMessage;
use crate::*;
use once_cell::sync::Lazy;
use pollster::FutureExt;
use regex::Regex;
use std::cell::Cell;
use std::error::Error;
use std::num;
use std::sync::{Arc, Mutex};
use winit;
use winit::event_loop::{ActiveEventLoop, EventLoop};
use winit::{dpi, window};

pub(crate) struct Screen {
    pub window: Arc<window::Window>,
    pub surface: wgpu::Surface<'static>,
    surface_config_data: SurfaceConfigData,
    surface_size: Mutex<Cell<(num::NonZeroU32, num::NonZeroU32)>>,
    pub device: wgpu::Device,
    backend: wgpu::Backend,
    pub queue: wgpu::Queue,
}

impl Screen {
    pub fn new(
        config: &ScreenConfig,
        event_loop: &ActiveEventLoop,
    ) -> Result<Screen, Box<dyn Error>> {
        let window = platform::create_window(&config, event_loop)?;
        if let Some(monitor) = window.current_monitor() {
            let monitor_size = monitor.size();
            let window_size = window.outer_size();
            let pos = dpi::PhysicalPosition::new(
                ((monitor_size.width - window_size.width) / 2u32) as i32,
                ((monitor_size.height - window_size.height) / 2u32) as i32,
            );
            window.set_outer_position(dpi::Position::Physical(pos));
        }
        window.set_ime_allowed(true);
        window.focus_window();
        Self::initialize(window, &config.backend, &config.present_mode.to_wgpu_type())
    }

    fn initialize(
        window: window::Window,
        backends: &wgpu::Backends,
        present_mode: &wgpu::PresentMode,
    ) -> Result<Screen, Box<dyn Error>> {
        let size = window.inner_size();
        let instance = wgpu::Instance::new(&wgpu::InstanceDescriptor {
            backends: *backends,
            flags: wgpu::InstanceFlags::empty(), // TODO: set flags for debugging
            backend_options: wgpu::BackendOptions::default(),
        });
        let window = Arc::new(window);
        let surface = instance.create_surface(window.clone())?;
        let adapter = instance
            .request_adapter(&wgpu::RequestAdapterOptions {
                power_preference: wgpu::PowerPreference::None,
                compatible_surface: Some(&surface),
                force_fallback_adapter: false,
            })
            .block_on()?;
        let (device, queue) = adapter
            .request_device(&wgpu::DeviceDescriptor {
                required_features: wgpu::Features::ADDRESS_MODE_CLAMP_TO_BORDER,
                required_limits: wgpu::Limits::default(),
                memory_hints: wgpu::MemoryHints::default(),
                trace: wgpu::Trace::Off,
                label: None,
            })
            .block_on()?;
        device.on_uncaptured_error(Box::new(|error| {
            static ANCI_ESC_SEQ_DECORATION: Lazy<Regex> =
                Lazy::new(|| Regex::new("\x1b\\[[0-9;]*m").unwrap());
            let message: String = error.to_string();
            // Some error messages contain decorations of ANSI escape sequences.
            // They should be removed.
            let message = ANCI_ESC_SEQ_DECORATION.replace_all(&message, "");
            crate::engine::Engine::on_unhandled_error(&message);
        }));
        let surface_config = {
            let surface_caps = surface.get_capabilities(&adapter);
            if surface_caps.present_modes.contains(present_mode) == false {
                return Err(format!(
                    "PresentMode '{:?}' is not supported in the current instance",
                    *present_mode
                )
                .into());
            }

            let surface_format = surface_caps
                .formats
                .iter()
                .copied()
                .filter(|f| f.is_srgb())
                .next()
                .unwrap_or(surface_caps.formats[0]);

            new_default_surface_config(
                surface_format,
                size.width,
                size.height,
                *present_mode,
                surface_caps.alpha_modes[0],
            )
        };
        surface.configure(&device, &surface_config);
        let size = (surface_config.width, surface_config.height);
        Ok(Screen {
            window,
            surface,
            surface_config_data: surface_config.into(),
            surface_size: Mutex::new(Cell::new((
                num::NonZeroU32::new(size.0).expect("cannot set 0 to surface width"),
                num::NonZeroU32::new(size.1).expect("cannot set 0 to surface height"),
            ))),
            device,
            backend: adapter.get_info().backend,
            queue,
        })
    }

    pub fn get_info(&self) -> ScreenInfo {
        let format = self.surface_config_data.format;
        ScreenInfo {
            backend: self.backend,
            surface_format: format.try_into().ok().into(),
        }
    }

    pub fn set_inner_size(&self, width: num::NonZeroU32, height: num::NonZeroU32) {
        let size = dpi::PhysicalSize::<u32>::new(width.into(), height.into());
        _ = self.window.request_inner_size(size);
    }

    pub fn resize_surface(&self, width: u32, height: u32) {
        if let (Some(width), Some(height)) =
            (num::NonZeroU32::new(width), num::NonZeroU32::new(height))
        {
            let lock = self.surface_size.lock().unwrap();
            lock.set((width, height));
            let config = self.surface_config_data.to_wgpu_type(width, height);
            self.surface.configure(&self.device, &config);
        }
    }
}

#[repr(transparent)]
#[derive(Clone, Copy, PartialEq, Eq, Hash)]
pub(crate) struct ScreenId(usize);

#[derive(Clone, Copy)]
struct SurfaceConfigData {
    pub usage: wgpu::TextureUsages,
    pub format: wgpu::TextureFormat,
    pub present_mode: wgpu::PresentMode,
    pub alpha_mode: wgpu::CompositeAlphaMode,
}

impl SurfaceConfigData {
    pub fn to_wgpu_type(
        &self,
        width: num::NonZeroU32,
        height: num::NonZeroU32,
    ) -> wgpu::SurfaceConfiguration {
        wgpu::SurfaceConfiguration {
            usage: self.usage,
            format: self.format,
            width: width.into(),
            height: height.into(),
            present_mode: self.present_mode,
            alpha_mode: self.alpha_mode,
            view_formats: vec![],
            desired_maximum_frame_latency: 2,
        }
    }
}

impl From<wgpu::SurfaceConfiguration> for SurfaceConfigData {
    fn from(x: wgpu::SurfaceConfiguration) -> Self {
        Self {
            usage: x.usage,
            format: x.format,
            present_mode: x.present_mode,
            alpha_mode: x.alpha_mode,
        }
    }
}

fn new_default_surface_config(
    format: wgpu::TextureFormat,
    width: u32,
    height: u32,
    present_mode: wgpu::PresentMode,
    alpha_mode: wgpu::CompositeAlphaMode,
) -> wgpu::SurfaceConfiguration {
    wgpu::SurfaceConfiguration {
        usage: wgpu::TextureUsages::RENDER_ATTACHMENT,
        format,
        width,
        height,
        present_mode,
        alpha_mode,
        view_formats: vec![],
        desired_maximum_frame_latency: 2,
    }
}
