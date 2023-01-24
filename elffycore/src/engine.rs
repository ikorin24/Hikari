use crate::error::*;
use crate::traceln;
use crate::types::*;
use pollster::FutureExt;
use std::cell::Cell;
use std::error::Error;
use std::num;
use wgpu::util::DeviceExt;
use winit;
use winit::event_loop::EventLoopWindowTarget;
use winit::{dpi, event, event_loop, window};

/// Start the engine.
/// The function never returns except error.
pub(crate) fn engine_start(
    engine_config: &EngineCoreConfig,
    screen_config: &HostScreenConfig,
) -> Box<dyn Error> {
    env_logger::init();
    if let Some(err_dispatcher) = engine_config.err_dispatcher {
        set_err_dispatcher(err_dispatcher);
    }
    let event_loop = event_loop::EventLoop::new();
    // let mut screens: Vec<Box<HostScreen>> = vec![];
    // let first_screen = match HostScreen::new(&screen_config, &event_loop) {
    //     Ok(screen) => screens.push_get_ref(screen),
    //     Err(err) => {
    //         return err;
    //     }
    // };

    let screen = match HostScreen::new(&screen_config, &event_loop) {
        Ok(screen) => Box::new(screen),
        Err(err) => {
            return err;
        }
    };
    {
        let on_screen_init = engine_config.on_screen_init;
        let callbacks = on_screen_init(screen.as_ref(), &screen.get_info());
        screen.set_callbacks(callbacks);
    }

    // let callbacks = (engine_config.on_screen_init)(first_screen, &first_screen.get_info());
    // first_screen.set_callbacks(callbacks);

    event_loop.run(move |event, event_loop, control_flow| {
        // screens.iter().for_each(|screen| {
        //     screen.handle_event(&event, event_loop, control_flow);
        // });
        screen.handle_event(&event, event_loop, control_flow);
    });
}

#[cfg(target_os = "windows")]
fn create_window(
    config: &HostScreenConfig,
    event_loop: &event_loop::EventLoop<()>,
) -> Result<window::Window, Box<dyn Error>> {
    use winit::platform::windows::WindowBuilderExtWindows;

    let window = window::WindowBuilder::new()
        .with_title(config.title.as_str()?)
        .with_inner_size(dpi::Size::Physical(dpi::PhysicalSize::new(
            config.width,
            config.height,
        )))
        .with_theme(Some(window::Theme::Light))
        .build(&event_loop)?;
    match config.style {
        WindowStyle::Default => {
            window.set_resizable(true);
        }
        WindowStyle::Fixed => {
            window.set_resizable(false);
        }
        WindowStyle::Fullscreen => {
            window.set_fullscreen(None);
        }
    }
    Ok(window)
}

#[cfg(target_os = "macos")]
fn create_window(style: &WindowStyle) -> (window::Window, event_loop::EventLoop<()>) {
    use winit::platform::macos::WindowExtMacOS;

    let event_loop = event_loop::EventLoop::new();
    let window = window::WindowBuilder::new()
        .with_title("Elffy")
        .with_inner_size(dpi::Size::Physical(dpi::PhysicalSize::new(1280, 720)))
        .build(&event_loop)
        .unwrap();
    match style {
        WindowStyle::Default => {
            window.set_resizable(true);
        }
        WindowStyle::Fixed => {
            window.set_resizable(false);
        }
        WindowStyle::Fullscreen => {
            window.set_simple_fullscreen(true);
        }
    }
    (window, event_loop)
}

#[derive(Clone, Copy)]
struct SurfaceConfigData {
    pub usage: wgpu::TextureUsages,
    pub format: wgpu::TextureFormat,
    pub present_mode: wgpu::PresentMode,
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

impl SurfaceConfigData {
    pub fn to_surface_config(
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

pub(crate) struct HostScreen {
    window: window::Window,
    surface: wgpu::Surface,
    surface_config_data: SurfaceConfigData,
    surface_size: Cell<(num::NonZeroU32, num::NonZeroU32)>,
    device: wgpu::Device,
    backend: wgpu::Backend,
    queue: wgpu::Queue,
    on_render: Cell<Option<HostScreenRenderFn>>,
}

impl HostScreen {
    const CLEAR_COLOR: wgpu::Color = wgpu::Color {
        r: 0.0,
        g: 0.0,
        b: 0.0,
        a: 0.0,
    };

    pub fn new(
        config: &HostScreenConfig,
        event_loop: &event_loop::EventLoop<()>,
    ) -> Result<HostScreen, Box<dyn Error>> {
        let window = create_window(&config, &event_loop)?;
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
            surface_size: Cell::new((
                num::NonZeroU32::new(size.0).expect("cannot set 0 to surface width"),
                num::NonZeroU32::new(size.1).expect("cannot set 0 to surface height"),
            )),
            device,
            backend: adapter.get_info().backend,
            queue,
            on_render: Cell::new(None),
        })
    }

    pub fn get_info(&self) -> HostScreenInfo {
        let format = self.surface_config_data.format;
        HostScreenInfo {
            backend: self.backend,
            surface_format: format.try_into().ok().into(),
        }
    }

    pub fn write_texture(
        &self,
        texture: &ImageCopyTexture,
        data: &[u8],
        data_layout: &wgpu::ImageDataLayout,
        size: &wgpu::Extent3d,
    ) {
        traceln!(
            r"write_texture(
    texture: {:#?},
    data: &[u8; {:?}],
    data_layout: {:#?},
    size: {:#?},
);",
            texture.to_wgpu_type(),
            data.len(),
            data_layout,
            size,
        );
        self.queue
            .write_texture(texture.to_wgpu_type(), data, *data_layout, *size)
    }

    pub fn write_buffer(&self, buffer: &wgpu::Buffer, offset: u64, data: &[u8]) {
        traceln!(
            r"write_buffer(
    buffer: {:#?},
    offset: {:#?},
    data: &[u8; {:?}],
);",
            buffer,
            offset,
            data.len(),
        );
        self.queue.write_buffer(buffer, offset, data)
    }

    pub fn create_bind_group_layout(
        &self,
        desc: &wgpu::BindGroupLayoutDescriptor,
    ) -> Box<wgpu::BindGroupLayout> {
        traceln!(
            r"create_bind_group_layout(
    desc: {:#?},
);",
            desc
        );
        let layout = self.device.create_bind_group_layout(desc);
        Box::new(layout)
    }

    pub fn create_bind_group(&self, desc: &wgpu::BindGroupDescriptor) -> Box<wgpu::BindGroup> {
        traceln!(
            r"create_bind_group(
    desc: {:#?},
);",
            desc
        );
        let bind_group = self.device.create_bind_group(desc);
        Box::new(bind_group)
    }

    pub fn create_sampler(&self, desc: &wgpu::SamplerDescriptor) -> Box<wgpu::Sampler> {
        traceln!(
            r"create_sampler(
    desc: {:#?},
);",
            desc
        );
        let sampler = self.device.create_sampler(desc);
        Box::new(sampler)
    }

    pub fn create_pipeline_layout(
        &self,
        desc: &wgpu::PipelineLayoutDescriptor,
    ) -> Box<wgpu::PipelineLayout> {
        traceln!(
            r"create_pipeline_layout(
    desc: {:#?},
);",
            desc
        );
        Box::new(self.device.create_pipeline_layout(desc))
    }

    pub fn create_render_pipeline(
        &self,
        desc: &wgpu::RenderPipelineDescriptor,
    ) -> Box<wgpu::RenderPipeline> {
        traceln!(
            r"create_render_pipeline(
    desc: {:#?},
);",
            desc
        );
        Box::new(self.device.create_render_pipeline(desc))
    }

    pub fn create_shader_module(&self, shader_source: &str) -> Box<wgpu::ShaderModule> {
        traceln!(
            r"create_shader_module(
    shader_source: &str (len={:?}),
);",
            shader_source.len()
        );
        let shader = self
            .device
            .create_shader_module(wgpu::ShaderModuleDescriptor {
                label: None,
                source: wgpu::ShaderSource::Wgsl(shader_source.into()),
            });
        Box::new(shader)
    }

    pub fn create_buffer_init(
        &self,
        contents: &[u8],
        usage: wgpu::BufferUsages,
    ) -> Box<wgpu::Buffer> {
        traceln!(
            r"create_buffer_init(
    contents: &[u8; {:?}],
    usage: {:#?},
);",
            contents.len(),
            usage,
        );
        let buffer = self
            .device
            .create_buffer_init(&wgpu::util::BufferInitDescriptor {
                label: None,
                contents,
                usage,
            });
        Box::new(buffer)
    }

    pub fn create_texture(&self, desc: &wgpu::TextureDescriptor) -> Box<wgpu::Texture> {
        traceln!(
            r"create_texture(
    desc: {:#?},
);",
            desc,
        );
        Box::new(self.device.create_texture(desc))
    }

    pub fn create_texture_with_data(
        &self,
        desc: &wgpu::TextureDescriptor,
        data: &[u8],
    ) -> Box<wgpu::Texture> {
        traceln!(
            r"create_texture_with_data(
    desc: {:#?},
    data: &[u8; {:?}]
);",
            desc,
            data.len()
        );
        let texture = self
            .device
            .create_texture_with_data(&self.queue, desc, data);
        Box::new(texture)
    }

    pub fn set_callbacks(&self, callbacks: HostScreenCallbacks) {
        self.on_render.set(callbacks.on_render);
    }

    pub fn get_inner_size(&self) -> (u32, u32) {
        self.window.inner_size().into()
    }

    pub fn set_inner_size(&self, width: num::NonZeroU32, height: num::NonZeroU32) {
        let size = dpi::PhysicalSize::<u32>::new(width.into(), height.into());
        self.window.set_inner_size(size);
    }

    pub fn handle_event(
        &self,
        event: &winit::event::Event<()>,
        _event_loop: &EventLoopWindowTarget<()>,
        control_flow: &mut winit::event_loop::ControlFlow,
    ) {
        use event::*;

        match event {
            Event::WindowEvent {
                ref event,
                window_id,
            } if *window_id == self.window.id() => match event {
                WindowEvent::CloseRequested
                | WindowEvent::KeyboardInput {
                    input:
                        KeyboardInput {
                            state: ElementState::Pressed,
                            virtual_keycode: Some(VirtualKeyCode::Escape),
                            ..
                        },
                    ..
                } => *control_flow = winit::event_loop::ControlFlow::Exit,
                WindowEvent::KeyboardInput {
                    input:
                        KeyboardInput {
                            state: ElementState::Pressed,
                            virtual_keycode: Some(VirtualKeyCode::Space),
                            ..
                        },
                    ..
                } => {}
                WindowEvent::Resized(physical_size) => {
                    self.resize_surface(physical_size.width, physical_size.height);
                }
                WindowEvent::ScaleFactorChanged { new_inner_size, .. } => {
                    self.resize_surface(new_inner_size.width, new_inner_size.height);
                }
                _ => {}
            },
            Event::RedrawRequested(window_id) if *window_id == self.window.id() => {
                match self.render() {
                    Ok(_) => {}
                    Err(wgpu::SurfaceError::Lost) => {
                        let size = self.window.inner_size();
                        self.resize_surface(size.width, size.height);
                    }
                    Err(e) => {
                        eprintln!("{:?}", e);
                    }
                }
            }
            Event::MainEventsCleared => {
                self.window.request_redraw();
            }
            _ => {}
        }
    }

    fn render(&self) -> Result<(), wgpu::SurfaceError> {
        // `get_current_texture` function will wait for the surface
        // to provide a new SurfaceTexture that we will render to.
        let output = self.surface.get_current_texture()?;
        let view = output
            .texture
            .create_view(&wgpu::TextureViewDescriptor::default());
        let mut command_encoder =
            self.device
                .create_command_encoder(&wgpu::CommandEncoderDescriptor {
                    label: Some("Render Encoder"),
                });
        {
            let mut render_pass = command_encoder.begin_render_pass(&wgpu::RenderPassDescriptor {
                label: Some("Render Pass"),
                color_attachments: &[Some(wgpu::RenderPassColorAttachment {
                    view: &view,
                    resolve_target: None,
                    ops: wgpu::Operations {
                        load: wgpu::LoadOp::Clear(Self::CLEAR_COLOR),
                        store: true,
                    },
                })],
                depth_stencil_attachment: None,
            });

            if let Some(on_render) = self.on_render.get() {
                on_render(self, &mut render_pass);
            }
        } // `render_pass` drops here.

        self.queue.submit(std::iter::once(command_encoder.finish()));
        output.present();

        Ok(())
    }

    fn resize_surface(&self, width: u32, height: u32) {
        if let (Some(width), Some(height)) =
            (num::NonZeroU32::new(width), num::NonZeroU32::new(height))
        {
            self.surface_size.set((width, height));
            let config = self.surface_config_data.to_surface_config(width, height);
            self.surface.configure(&self.device, &config);
        }
    }
}
