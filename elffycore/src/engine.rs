use crate::types::*;
use pollster::FutureExt;
use std::error::Error;
use std::ptr;
use wgpu::util::DeviceExt;
use winit;
use winit::event_loop::EventLoopWindowTarget;
use winit::{dpi, event, event_loop, window};

pub(crate) fn engine_start(
    engine_config: &EngineCoreConfig,
    screen_config: &HostScreenConfig,
) -> ! {
    env_logger::init();
    if let Some(err_dispatcher) = engine_config.err_dispatcher {
        crate::set_err_dispatcher(err_dispatcher);
    }
    let event_loop = event_loop::EventLoop::new();
    let mut screens: Vec<Box<HostScreen>> = vec![];
    let first_screen = screens.push_get_mut(HostScreen::new(&screen_config, &event_loop));
    let callbacks = (engine_config.on_screen_init)(first_screen, &first_screen.get_info());
    first_screen.set_callbacks(callbacks);
    event_loop.run(move |event, event_loop, control_flow| {
        screens.iter_mut().for_each(|screen| {
            screen.handle_event(&event, event_loop, control_flow);
        });
    });
}

#[cfg(target_os = "windows")]
fn create_window(
    config: &HostScreenConfig,
    event_loop: &event_loop::EventLoop<()>,
) -> window::Window {
    use winit::platform::windows::WindowBuilderExtWindows;

    let window = window::WindowBuilder::new()
        .with_title(config.title.as_str().unwrap())
        .with_inner_size(dpi::Size::Physical(dpi::PhysicalSize::new(
            config.width,
            config.height,
        )))
        .with_theme(Some(window::Theme::Light))
        .build(&event_loop)
        .unwrap();
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
    window
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

pub(crate) struct HostScreen {
    window: window::Window,
    surface: wgpu::Surface,
    surface_config: wgpu::SurfaceConfiguration,
    device: wgpu::Device,
    backend: wgpu::Backend,
    queue: wgpu::Queue,
    pipeline_layouts: Vec<Box<wgpu::PipelineLayout>>,
    pipelines: Vec<Box<wgpu::RenderPipeline>>,
    buffers: Vec<Box<wgpu::Buffer>>,
    textures: Vec<Box<wgpu::Texture>>,
    shaders: Vec<Box<wgpu::ShaderModule>>,
    bind_group_layouts: Vec<Box<wgpu::BindGroupLayout>>,
    bind_groups: Vec<Box<wgpu::BindGroup>>,
    samplers: Vec<Box<wgpu::Sampler>>,
    texture_views: Vec<Box<wgpu::TextureView>>,
    callbacks: HostScreenCallbacks,
}

impl HostScreen {
    const CLEAR_COLOR: wgpu::Color = wgpu::Color {
        r: 0.0,
        g: 0.0,
        b: 0.0,
        a: 0.0,
    };

    pub fn new(config: &HostScreenConfig, event_loop: &event_loop::EventLoop<()>) -> HostScreen {
        let window = create_window(&config, &event_loop);
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
        Self::initialize(window, &config.backend).unwrap()
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
        device.on_uncaptured_error(|err: wgpu::Error| {
            let message = format!("{:?}", err);
            if let Some(err_dispatcher) = crate::get_err_dispatcher() {
                let id = crate::generate_message_id();
                let message_bytes = message.as_bytes();
                err_dispatcher(id, message_bytes.as_ptr(), message_bytes.len());
            } else {
                eprintln!("{}", message);
            }
        });
        let surface_config = wgpu::SurfaceConfiguration {
            usage: wgpu::TextureUsages::RENDER_ATTACHMENT,
            format: surface.get_supported_formats(&adapter)[0],
            width: size.width,
            height: size.height,
            present_mode: wgpu::PresentMode::Fifo,
        };

        surface.configure(&device, &surface_config);
        Ok(HostScreen {
            window,
            surface_config,
            surface,
            device,
            backend: adapter.get_info().backend,
            queue,
            pipeline_layouts: vec![],
            pipelines: vec![],
            buffers: vec![],
            textures: vec![],
            shaders: vec![],
            bind_group_layouts: vec![],
            bind_groups: vec![],
            samplers: vec![],
            texture_views: vec![],
            callbacks: HostScreenCallbacks::default(),
        })
    }

    pub fn get_info(&self) -> HostScreenInfo {
        HostScreenInfo {
            backend: self.backend,
            surface_format: self.surface_config.format.try_into().ok().into(),
        }
    }

    pub fn write_texture(
        &self,
        texture: &ImageCopyTexture,
        data: &[u8],
        data_layout: &wgpu::ImageDataLayout,
        size: &wgpu::Extent3d,
    ) {
        self.queue
            .write_texture(texture.to_wgpu_type(), data, *data_layout, *size)
    }

    pub fn create_bind_group_layout(
        &mut self,
        desc: &wgpu::BindGroupLayoutDescriptor,
    ) -> &wgpu::BindGroupLayout {
        let layout = self.device.create_bind_group_layout(desc);
        self.bind_group_layouts.push_get_ref(layout)
    }

    pub fn destroy_bind_group_layout(&mut self, layout: &wgpu::BindGroupLayout) -> bool {
        self.bind_group_layouts.swap_remove_by_ref(layout).is_some()
    }

    pub fn create_bind_group(
        &mut self,
        bind_group_desc: &wgpu::BindGroupDescriptor,
    ) -> &wgpu::BindGroup {
        let bind_group = self.device.create_bind_group(bind_group_desc);
        self.bind_groups.push_get_ref(bind_group)
    }

    pub fn destroy_bind_group(&mut self, bind_group: &wgpu::BindGroup) -> bool {
        self.bind_groups.swap_remove_by_ref(bind_group).is_some()
    }

    pub fn create_texture_view(
        &mut self,
        texture: &wgpu::Texture,
        desc: &wgpu::TextureViewDescriptor,
    ) -> &wgpu::TextureView {
        let texture_view = texture.create_view(desc);
        self.texture_views.push_get_ref(texture_view)
    }

    pub fn destroy_texture_view(&mut self, texture_view: &wgpu::TextureView) -> bool {
        self.texture_views
            .swap_remove_by_ref(texture_view)
            .is_some()
    }

    pub fn create_sampler(&mut self, desc: &wgpu::SamplerDescriptor) -> &wgpu::Sampler {
        let sampler = self.device.create_sampler(desc);
        self.samplers.push_get_ref(sampler)
    }
    pub fn destroy_sampler(&mut self, sampler: &wgpu::Sampler) -> bool {
        self.samplers.swap_remove_by_ref(sampler).is_some()
    }

    pub fn create_pipeline_layout(
        &mut self,
        layout_desc: &wgpu::PipelineLayoutDescriptor,
    ) -> &wgpu::PipelineLayout {
        let layout = self.device.create_pipeline_layout(layout_desc);
        self.pipeline_layouts.push_get_ref(layout)
    }

    pub fn destroy_pipeline_layout(&mut self, layout: &wgpu::PipelineLayout) -> bool {
        self.pipeline_layouts.swap_remove_by_ref(layout).is_some()
    }

    pub fn create_render_pipeline(
        &mut self,
        desc: &wgpu::RenderPipelineDescriptor,
    ) -> &wgpu::RenderPipeline {
        let pipeline = self.device.create_render_pipeline(desc);
        self.pipelines.push_get_ref(pipeline)
    }

    pub fn destroy_render_pipeline(&mut self, render_pipeline: &wgpu::RenderPipeline) -> bool {
        self.pipelines.swap_remove_by_ref(render_pipeline).is_some()
    }

    pub fn create_shader_module(&mut self, shader_source: &str) -> &wgpu::ShaderModule {
        let shader = self
            .device
            .create_shader_module(wgpu::ShaderModuleDescriptor {
                label: None,
                source: wgpu::ShaderSource::Wgsl(shader_source.into()),
            });
        self.shaders.push_get_ref(shader)
    }

    pub fn destroy_shader_module(&mut self, shader: &wgpu::ShaderModule) -> bool {
        self.shaders.swap_remove_by_ref(shader).is_some()
    }

    pub fn create_buffer_init(
        &mut self,
        contents: &[u8],
        usage: wgpu::BufferUsages,
    ) -> &wgpu::Buffer {
        let buffer = self
            .device
            .create_buffer_init(&wgpu::util::BufferInitDescriptor {
                label: None,
                contents,
                usage,
            });
        self.buffers.push_get_ref(buffer)
    }

    pub fn destroy_buffer(&mut self, buffer: &wgpu::Buffer) -> bool {
        self.buffers
            .swap_remove_by_ref(buffer)
            .map(|buf| buf.destroy())
            .is_some()
    }

    pub fn create_texture<'screen>(
        &'screen mut self,
        desc: &wgpu::TextureDescriptor,
    ) -> &'screen wgpu::Texture {
        let texture = self.device.create_texture(desc);
        self.textures.push_get_ref(texture)
    }

    pub fn create_texture_with_data<'screen>(
        &'screen mut self,
        desc: &wgpu::TextureDescriptor,
        data: &[u8],
    ) -> &'screen wgpu::Texture {
        let texture = self
            .device
            .create_texture_with_data(&self.queue, desc, data);
        self.textures.push_get_ref(texture)
    }

    pub fn destroy_texture(&mut self, texture: &wgpu::Texture) -> bool {
        self.textures
            .swap_remove_by_ref(texture)
            .map(|tex| tex.destroy())
            .is_some()
    }

    pub fn set_callbacks(&mut self, callbacks: HostScreenCallbacks) {
        self.callbacks = callbacks;
    }

    pub fn handle_event(
        &mut self,
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
                    self.resize(*physical_size);
                }
                WindowEvent::ScaleFactorChanged { new_inner_size, .. } => {
                    self.resize(**new_inner_size);
                }
                _ => {}
            },
            Event::RedrawRequested(window_id) if *window_id == self.window.id() => {
                match self.render() {
                    Ok(_) => {}
                    Err(wgpu::SurfaceError::Lost) => {
                        self.resize(self.window.inner_size());
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

    fn render(&mut self) -> Result<(), wgpu::SurfaceError> {
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

            if let Some(on_render) = &self.callbacks.on_render {
                on_render(self, &mut render_pass);
            }
        } // `render_pass` drops here.

        self.queue.submit(std::iter::once(command_encoder.finish()));
        output.present();

        Ok(())
    }

    pub fn resize(&mut self, new_size: winit::dpi::PhysicalSize<u32>) {
        if new_size.width > 0 && new_size.height > 0 {
            self.surface_config.width = new_size.width;
            self.surface_config.height = new_size.height;
            self.surface.configure(&self.device, &self.surface_config);
        }
    }
}

trait VecBox<T> {
    fn push_get_ref(&mut self, value: T) -> &T;
    fn push_get_mut(&mut self, value: T) -> &mut T;

    fn swap_remove_by_ref(&mut self, value_ref: &T) -> Option<T>;
}

impl<T> VecBox<T> for Vec<Box<T>> {
    fn push_get_ref(&mut self, value: T) -> &T {
        self.push(Box::new(value));
        self.last().unwrap().as_ref()
    }

    fn push_get_mut(&mut self, value: T) -> &mut T {
        self.push(Box::new(value));
        self.last_mut().unwrap()
    }

    fn swap_remove_by_ref(&mut self, value_ref: &T) -> Option<T> {
        let index = self.iter().position(|x| ptr::eq(value_ref, x.as_ref()));
        match index {
            Some(index) => Some(*self.swap_remove(index)),
            None => None,
        }
    }
}
