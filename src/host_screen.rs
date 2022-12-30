use pollster::FutureExt;
use std::{fmt::Display, str::FromStr};
use wgpu::util::DeviceExt;
use winit::{
    event::*,
    event_loop::{ControlFlow, EventLoop},
    window::{Window, WindowBuilder},
};

use crate::{
    renderer::{HostScreen, LayoutedVertex},
    HostScreenInitFn,
};

#[repr(C)]
#[derive(Copy, Clone, Debug, bytemuck::Pod, bytemuck::Zeroable)]
struct Vertex {
    position: [f32; 3],
    color: [f32; 3],
}

impl LayoutedVertex for Vertex {
    fn get_layout() -> wgpu::VertexBufferLayout<'static> {
        wgpu::VertexBufferLayout {
            array_stride: std::mem::size_of::<Vertex>() as wgpu::BufferAddress,
            step_mode: wgpu::VertexStepMode::Vertex,
            attributes: &[
                wgpu::VertexAttribute {
                    offset: 0,
                    shader_location: 0,
                    format: wgpu::VertexFormat::Float32x3,
                },
                wgpu::VertexAttribute {
                    offset: std::mem::size_of::<[f32; 3]>() as wgpu::BufferAddress,
                    shader_location: 1,
                    format: wgpu::VertexFormat::Float32x3,
                },
            ],
        }
    }
}

#[allow(dead_code)]
const VERTICES: &[Vertex] = &[
    Vertex {
        position: [0.0, 0.5, 0.0],
        color: [1.0, 0.0, 0.0],
    },
    Vertex {
        position: [-0.5, -0.5, 0.0],
        color: [0.0, 1.0, 0.0],
    },
    Vertex {
        position: [0.5, -0.5, 0.0],
        color: [0.0, 0.0, 1.0],
    },
];

enum WindowStyle {
    Default,
    Fixed,
    Fullscreen,
}

impl FromStr for WindowStyle {
    type Err = ParseEnumError;

    fn from_str(s: &str) -> Result<Self, Self::Err> {
        match s {
            "Default" => Ok(WindowStyle::Default),
            "Fixed" => Ok(WindowStyle::Fixed),
            "Fullscreen" => Ok(WindowStyle::Fullscreen),
            _ => Err(ParseEnumError {
                string: s.to_owned(),
            }),
        }
    }
}

#[derive(Debug)]
struct ParseEnumError {
    pub string: String,
}

impl Display for ParseEnumError {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(f, "cannot parse str: {}", self.string)
    }
}

impl std::error::Error for ParseEnumError {}

#[cfg(target_os = "windows")]
fn set_window_style(window: &Window, style: &WindowStyle) {
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

// assert_eq_size!(winit::window::WindowId, usize);

// type EndEngineFn = extern "cdecl" fn();

// pub fn start_engine() -> ! {
//     start_engine_loop(|event_loop, event, control_flow| {})
// }

pub fn start_engine_start(init: HostScreenInitFn) -> ! {
    env_logger::init();
    let event_loop = EventLoop::new();

    let window = WindowBuilder::new().build(&event_loop).unwrap();
    set_window_style(&window, &WindowStyle::Default);
    window.focus_window();
    let first_screen = Box::new(HostScreen::new(window).unwrap_or_else(|err| panic!("{:?}", err)));
    let mut screens: Vec<Box<HostScreen>> = vec![first_screen];
    let first_screen = screens.last_mut().unwrap().as_mut();

    // let mut callbacks = HostScreenCallbacks::default();
    let callbacks = init(first_screen);
    first_screen.set_callbacks(callbacks);
    // callbacks.on_render.unwrap()(first_screen);

    // let vertex_buffer =
    //     first_screen
    //         .get_device()
    //         .create_buffer_init(&wgpu::util::BufferInitDescriptor {
    //             label: Some("Vertex Buffer"),
    //             contents: bytemuck::cast_slice(VERTICES),
    //             usage: wgpu::BufferUsages::VERTEX,
    //         });
    event_loop.run(move |event, event_loop, control_flow| {
        screens.iter_mut().for_each(|screen| {
            screen.handle_event(&event, event_loop, control_flow);
        });
    });

    // event_loop.run(move |event, event_loop, control_flow| match event {
    //     Event::WindowEvent {
    //         ref event,
    //         window_id,
    //     } if window_id == renderer.window_id() => match event {
    //         WindowEvent::CloseRequested
    //         | WindowEvent::KeyboardInput {
    //             input:
    //                 KeyboardInput {
    //                     state: ElementState::Pressed,
    //                     virtual_keycode: Some(VirtualKeyCode::Escape),
    //                     ..
    //                 },
    //             ..
    //         } => *control_flow = ControlFlow::Exit,
    //         WindowEvent::KeyboardInput {
    //             input:
    //                 KeyboardInput {
    //                     state: ElementState::Pressed,
    //                     virtual_keycode: Some(VirtualKeyCode::Space),
    //                     ..
    //                 },
    //             ..
    //         } => {}
    //         WindowEvent::Resized(physical_size) => {
    //             renderer.resize(*physical_size);
    //         }
    //         WindowEvent::ScaleFactorChanged { new_inner_size, .. } => {
    //             renderer.resize(**new_inner_size);
    //         }
    //         _ => {}
    //     },
    //     Event::RedrawRequested(window_id) if window_id == renderer.window_id() => {
    //         let vertices = RenderTargetVertices {
    //             vertex_buffer: vertex_buffer.slice(..),
    //             vertices_range: 0..3,
    //             instances: 0..1,
    //         };
    //         match renderer.render(vertices) {
    //             Ok(_) => {}
    //             Err(wgpu::SurfaceError::Lost) => {
    //                 renderer.resize(renderer.get_window().inner_size());
    //             }
    //             Err(e) => {
    //                 eprintln!("{:?}", e);
    //             }
    //         }
    //     }
    //     Event::MainEventsCleared => {
    //         renderer.get_window().request_redraw();
    //     }
    //     _ => {}
    // });
}

#[allow(dead_code)]
pub fn start() -> Result<(), Box<dyn std::error::Error>> {
    env_logger::init();
    let event_loop = EventLoop::new();
    let window = WindowBuilder::new().build(&event_loop)?;

    // window.set_decorations(false);
    // window.set_visible(false);
    // window.set_fullscreen(Some(Fullscreen::Borderless(None)));
    // window.set_visible(true);
    // window.set_cursor_visible(false);

    set_window_style(&window, &WindowStyle::Default);
    window.focus_window();

    let size = window.inner_size();

    let instance = wgpu::Instance::new(wgpu::Backends::all());
    let surface = unsafe { instance.create_surface(&window) };
    let adapter = instance
        .request_adapter(&wgpu::RequestAdapterOptions {
            power_preference: wgpu::PowerPreference::default(),
            compatible_surface: Some(&surface),
            force_fallback_adapter: false,
        })
        .block_on()
        .ok_or("failed to unwrap".to_owned())?;
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

    let mut config = wgpu::SurfaceConfiguration {
        usage: wgpu::TextureUsages::RENDER_ATTACHMENT,
        format: surface.get_supported_formats(&adapter)[0],
        width: size.width,
        height: size.height,
        present_mode: wgpu::PresentMode::Fifo,
    };
    surface.configure(&device, &config);

    let resize = |device: &wgpu::Device,
                  surface: &wgpu::Surface,
                  config: &mut wgpu::SurfaceConfiguration,
                  new_size: winit::dpi::PhysicalSize<u32>| {
        if new_size.width > 0 && new_size.height > 0 {
            config.width = new_size.width;
            config.height = new_size.height;
            surface.configure(&device, &config);
        }
    };

    // device.create_buffer(&wgpu::BufferDescriptor{
    //     label: None,
    //     size: 10,
    //     usage: wgpu::BufferUsages::VERTEX,
    //     mapped_at_creation: false,
    // });

    let vertex_buffer = device.create_buffer_init(&wgpu::util::BufferInitDescriptor {
        label: Some("Vertex Buffer"),
        contents: bytemuck::cast_slice(VERTICES),
        usage: wgpu::BufferUsages::VERTEX,
    });

    let shader = device.create_shader_module(wgpu::ShaderModuleDescriptor {
        label: Some("Shader"),
        source: wgpu::ShaderSource::Wgsl(include_str!("shader.wgsl").into()),
    });

    let render_pipeline_layout = device.create_pipeline_layout(&wgpu::PipelineLayoutDescriptor {
        label: Some("Render Pipeline Layout"),
        bind_group_layouts: &[],
        push_constant_ranges: &[],
    });

    let render_pipeline = device.create_render_pipeline(&wgpu::RenderPipelineDescriptor {
        label: Some("Render Pipeline"),
        layout: Some(&render_pipeline_layout),
        vertex: wgpu::VertexState {
            module: &shader,
            entry_point: "vs_main",
            buffers: &[Vertex::get_layout()],
        },
        fragment: Some(wgpu::FragmentState {
            module: &shader,
            entry_point: "fs_main",
            targets: &[Some(wgpu::ColorTargetState {
                format: config.format,
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

    let render = |device: &wgpu::Device,
                  queue: &wgpu::Queue,
                  surface: &wgpu::Surface,
                  render_pipeline: &wgpu::RenderPipeline,
                  vertex_buffer: &wgpu::Buffer| {
        let output = surface.get_current_texture()?;
        let view = output
            .texture
            .create_view(&wgpu::TextureViewDescriptor::default());
        let mut encoder = device.create_command_encoder(&wgpu::CommandEncoderDescriptor {
            label: Some("Render Encoder"),
        });

        {
            let mut render_pass = encoder.begin_render_pass(&wgpu::RenderPassDescriptor {
                label: Some("Render Pass"),
                color_attachments: &[Some(wgpu::RenderPassColorAttachment {
                    view: &view,
                    resolve_target: None,
                    ops: wgpu::Operations {
                        load: wgpu::LoadOp::Clear(wgpu::Color {
                            r: 0.0,
                            g: 0.0,
                            b: 0.0,
                            a: 0.0,
                        }),
                        store: true,
                    },
                })],
                depth_stencil_attachment: None,
            });

            render_pass.set_pipeline(&render_pipeline);
            render_pass.set_vertex_buffer(0, vertex_buffer.slice(..));
            render_pass.draw(0..3, 0..1);
        }

        queue.submit(std::iter::once(encoder.finish()));
        output.present();

        Ok(())
    };

    event_loop.run(move |event, _, control_flow| match event {
        Event::WindowEvent {
            ref event,
            window_id,
        } if window_id == window.id() => match event {
            WindowEvent::CloseRequested
            | WindowEvent::KeyboardInput {
                input:
                    KeyboardInput {
                        state: ElementState::Pressed,
                        virtual_keycode: Some(VirtualKeyCode::Escape),
                        ..
                    },
                ..
            } => *control_flow = ControlFlow::Exit,
            WindowEvent::Resized(physical_size) => {
                resize(&device, &surface, &mut config, *physical_size);
            }
            WindowEvent::ScaleFactorChanged { new_inner_size, .. } => {
                resize(&device, &surface, &mut config, **new_inner_size);
            }
            _ => {}
        },
        Event::RedrawRequested(window_id) if window_id == window.id() => {
            match render(&device, &queue, &surface, &render_pipeline, &vertex_buffer) {
                Ok(_) => {}
                Err(wgpu::SurfaceError::Lost) => {
                    resize(&device, &surface, &mut config, size);
                }
                Err(e) => eprintln!("{:?}", e),
            }
        }
        Event::MainEventsCleared => {
            window.request_redraw();
        }
        _ => {}
    });
}

// type ResizedWindowEventFn = extern "cdecl" fn(screen_id: usize, width: u32, height: u32);
// type ClosingEventFn = extern "cdecl" fn(screen_id: usize) -> bool;
// type ClosedEventFn = extern "cdecl" fn(screen_id: usize);
// type InitializedEventFn = extern "cdecl" fn(screen_id: usize);

// #[repr(C)]
// struct HostScreenConfig {
//     pub on_resized: Option<ResizedWindowEventFn>,
//     pub on_closing: Option<ClosingEventFn>,
//     pub on_closed: Option<ClosedEventFn>,
//     pub on_initialized: Option<InitializedEventFn>,
// }
