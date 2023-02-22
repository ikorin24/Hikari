mod macos;
mod windows;

#[cfg(target_os = "macos")]
use macos as platform;
#[cfg(target_os = "windows")]
use windows as platform;

use crate::error_handler::*;
use crate::*;
use pollster::FutureExt;
use std::cell::Cell;
use std::error::Error;
use std::num;
use std::sync::Mutex;
use winit;
use winit::{dpi, event_loop, window};

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
        event_loop: &event_loop::EventLoop<()>,
    ) -> Result<HostScreen, Box<dyn Error>> {
        let window = platform::create_window(&config, &event_loop)?;
        if let Some(monitor) = window.current_monitor() {
            let monitor_size = monitor.size();
            let window_size = window.outer_size();
            let pos = dpi::PhysicalPosition::new(
                ((monitor_size.width - window_size.width) / 2u32) as i32,
                ((monitor_size.height - window_size.height) / 2u32) as i32,
            );
            window.set_outer_position(dpi::Position::Physical(pos));
        }
        window.focus_window();
        Self::initialize(window, &config.backend)
    }

    fn initialize(
        window: window::Window,
        backends: &wgpu::Backends,
    ) -> Result<HostScreen, Box<dyn Error>> {
        let size = window.inner_size();
        let instance = wgpu::Instance::new(*backends);
        let surface = unsafe { instance.create_surface(&window) };
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
        device.on_uncaptured_error(|err: wgpu::Error| dispatch_err(err));
        let surface_config = wgpu::SurfaceConfiguration {
            usage: wgpu::TextureUsages::RENDER_ATTACHMENT,
            format: surface.get_supported_formats(&adapter)[0],
            width: size.width,
            height: size.height,
            present_mode: wgpu::PresentMode::Fifo,
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
#[derive(Clone, Copy)]
pub(crate) struct HostScreenId(usize);

impl HostScreenId {
    pub fn new(screen: &Box<HostScreen>) -> Self {
        let screen_ref: &HostScreen = screen.as_ref();
        let screen_addr = screen_ref as *const HostScreen as usize;
        Self(screen_addr)
    }
}

#[derive(Clone, Copy)]
struct SurfaceConfigData {
    pub usage: wgpu::TextureUsages,
    pub format: wgpu::TextureFormat,
    pub present_mode: wgpu::PresentMode,
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
        }
    }
}

impl From<wgpu::SurfaceConfiguration> for SurfaceConfigData {
    fn from(x: wgpu::SurfaceConfiguration) -> Self {
        Self {
            usage: x.usage,
            format: x.format,
            present_mode: x.present_mode,
        }
    }
}
