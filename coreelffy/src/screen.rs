mod macos;
mod windows;

#[cfg(target_os = "macos")]
use macos as platform;
#[cfg(target_os = "windows")]
use windows as platform;

use crate::engine::ProxyMessage;
use crate::error_handler::*;
use crate::*;
use pollster::FutureExt;
use std::cell::Cell;
use std::error::Error;
use std::num;
use std::sync::Mutex;
use winit;
use winit::event_loop::EventLoopWindowTarget;
use winit::{dpi, window};

pub(crate) struct HostScreen {
    pub window: window::Window,
    pub surface: wgpu::Surface,
    surface_config_data: SurfaceConfigData,
    surface_size: Mutex<Cell<(num::NonZeroU32, num::NonZeroU32)>>,
    pub device: wgpu::Device,
    backend: wgpu::Backend,
    pub queue: wgpu::Queue,
}

impl HostScreen {
    pub fn new(
        config: &HostScreenConfig,
        event_loop: &EventLoopWindowTarget<ProxyMessage>,
    ) -> Result<HostScreen, Box<dyn Error>> {
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
        window.set_ime_position(dpi::PhysicalPosition::new(0, 0));
        window.focus_window();
        Self::initialize(window, &config.backend)
    }

    fn initialize(
        window: window::Window,
        backends: &wgpu::Backends,
    ) -> Result<HostScreen, Box<dyn Error>> {
        let size = window.inner_size();
        let instance = wgpu::Instance::new(wgpu::InstanceDescriptor {
            backends: *backends,
            dx12_shader_compiler: wgpu::Dx12Compiler::default(),
        });
        let surface = unsafe { instance.create_surface(&window)? };
        let adapter = instance
            .request_adapter(&wgpu::RequestAdapterOptions {
                power_preference: wgpu::PowerPreference::HighPerformance,
                compatible_surface: Some(&surface),
                force_fallback_adapter: false,
            })
            .block_on()
            .ok_or("no graphics card found available for the specified config".to_owned())?;
        let (device, queue) = adapter
            .request_device(
                &wgpu::DeviceDescriptor {
                    features: wgpu::Features::empty(),
                    limits: wgpu::Limits::default(),
                    label: None,
                },
                None,
            )
            .block_on()?;
        device.on_uncaptured_error(Box::new(|error| dispatch_err(error)));
        let surface_config = {
            let surface_caps = surface.get_capabilities(&adapter);
            let surface_format = surface_caps
                .formats
                .iter()
                .copied()
                .filter(|f| f.describe().srgb)
                .next()
                .unwrap_or(surface_caps.formats[0]);

            new_default_surface_config(
                surface_format,
                size.width,
                size.height,
                surface_caps.present_modes[0],
                surface_caps.alpha_modes[0],
            )
        };
        surface.configure(&device, &surface_config);
        let size = (surface_config.width, surface_config.height);
        Ok(HostScreen {
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

    pub fn get_info(&self) -> HostScreenInfo {
        let format = self.surface_config_data.format;
        HostScreenInfo {
            backend: self.backend,
            surface_format: format.try_into().ok().into(),
        }
    }

    pub fn set_inner_size(&self, width: num::NonZeroU32, height: num::NonZeroU32) {
        let size = dpi::PhysicalSize::<u32>::new(width.into(), height.into());
        self.window.set_inner_size(size);
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
    }
}
