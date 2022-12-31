use crate::types::*;
use pollster::FutureExt;
use std::error::Error;
use std::str::Utf8Error;
use wgpu::util::DeviceExt;
use wgpu::{Buffer, Device, Queue, RenderPipeline, Surface, SurfaceConfiguration, SurfaceError};
use winit;
use winit::dpi::{PhysicalPosition, PhysicalSize, Position, Size};
use winit::event;
use winit::event_loop::EventLoop;
use winit::platform::windows::WindowBuilderExtWindows;
use winit::window;
use winit::{event_loop::EventLoopWindowTarget, window::Window};

pub(crate) fn engine_start(init: HostScreenInitFn) -> ! {
    env_logger::init();
    let event_loop = EventLoop::new();
    let window = window::WindowBuilder::new()
        .with_title("Elffy")
        .with_inner_size(Size::Physical(PhysicalSize::new(1280, 720)))
        .with_theme(Some(window::Theme::Light))
        .build(&event_loop)
        .unwrap();
    set_window_style(&window, &WindowStyle::Default);

    if let Some(monitor) = window.current_monitor() {
        let monitor_size = monitor.size();
        let window_size = window.outer_size();
        let pos = PhysicalPosition::new(
            ((monitor_size.width - window_size.width) / 2u32) as i32,
            ((monitor_size.height - window_size.height) / 2u32) as i32,
        );
        window.set_outer_position(Position::Physical(pos));
    }
    window.focus_window();
    let first_screen = Box::new(HostScreen::new(window).unwrap_or_else(|err| panic!("{:?}", err)));
    let mut screens: Vec<Box<HostScreen>> = vec![first_screen];
    let first_screen = screens.last_mut().unwrap().as_mut();

    let callbacks = init(first_screen);
    first_screen.set_callbacks(callbacks);
    event_loop.run(move |event, event_loop, control_flow| {
        screens.iter_mut().for_each(|screen| {
            screen.handle_event(&event, event_loop, control_flow);
        });
    });
}

#[cfg(target_os = "windows")]
fn set_window_style(window: &window::Window, style: &WindowStyle) {
    match style {
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
}

#[cfg(target_os = "macos")]
fn set_window_style(window: &Window, style: &WindowStyle) {
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
}

pub(crate) struct HostScreen {
    window: Window,
    surface: Surface,
    surface_config: SurfaceConfiguration,
    device: Device,
    queue: Queue,
    pipelines: Vec<Box<RenderPipeline>>,
    buffers: Vec<Box<Buffer>>,
    callbacks: HostScreenCallbacks,
}

impl HostScreen {
    const CLEAR_COLOR: wgpu::Color = wgpu::Color {
        r: 0.0,
        g: 0.0,
        b: 0.0,
        a: 0.0,
    };

    pub fn new(window: Window) -> Result<HostScreen, Box<dyn Error>> {
        let size = window.inner_size();
        let instance = wgpu::Instance::new(wgpu::Backends::all());
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
            queue,
            pipelines: vec![],
            buffers: vec![],
            callbacks: HostScreenCallbacks::default(),
        })
    }

    pub fn add_render_pipeline(
        &mut self,
        rpi: &RenderPipelineInfo,
    ) -> Result<&RenderPipeline, Utf8Error> {
        let shader_source = rpi.shader_source.as_str()?;
        let shader = self
            .device
            .create_shader_module(wgpu::ShaderModuleDescriptor {
                label: Some("Shader"),
                source: wgpu::ShaderSource::Wgsl(shader_source.into()),
            });

        let render_pipeline_layout =
            self.device
                .create_pipeline_layout(&wgpu::PipelineLayoutDescriptor {
                    label: Some("Render Pipeline Layout"),
                    bind_group_layouts: &[],
                    push_constant_ranges: &[],
                });
        let render_pipeline = self
            .device
            .create_render_pipeline(&wgpu::RenderPipelineDescriptor {
                label: Some("Render Pipeline"),
                layout: Some(&render_pipeline_layout),
                vertex: wgpu::VertexState {
                    module: &shader,
                    entry_point: "vs_main",
                    buffers: &[rpi.vertex.to_vertex_buffer_layout()],
                },
                fragment: Some(wgpu::FragmentState {
                    module: &shader,
                    entry_point: "fs_main",
                    targets: &[Some(wgpu::ColorTargetState {
                        format: self.surface_config.format,
                        blend: Some(wgpu::BlendState::REPLACE),
                        write_mask: wgpu::ColorWrites::ALL,
                    })],
                }),
                primitive: wgpu::PrimitiveState {
                    topology: wgpu::PrimitiveTopology::TriangleList,
                    strip_index_format: None,
                    front_face: wgpu::FrontFace::Ccw,
                    cull_mode: Some(wgpu::Face::Back),
                    polygon_mode: wgpu::PolygonMode::Fill,
                    unclipped_depth: false,
                    conservative: false,
                },
                depth_stencil: None,
                multisample: wgpu::MultisampleState {
                    count: 1,
                    mask: !0,
                    alpha_to_coverage_enabled: false,
                },
                multiview: None,
            });
        self.pipelines.push(Box::new(render_pipeline));
        Ok(self.pipelines.last().unwrap().as_ref())
    }

    pub fn create_buffer_init(
        &mut self,
        contents: &[u8],
        usage: wgpu::BufferUsages,
    ) -> &mut Buffer {
        let buffer = self
            .device
            .create_buffer_init(&wgpu::util::BufferInitDescriptor {
                label: None,
                contents: contents,
                usage: usage,
            });
        self.buffers.push(Box::new(buffer));
        self.buffers.last_mut().unwrap().as_mut()
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

    fn render(&mut self) -> Result<(), SurfaceError> {
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
