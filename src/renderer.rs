use pollster::FutureExt;
use std::error::Error;
use std::str::Utf8Error;
use wgpu::{Device, Queue, RenderPipeline, Surface, SurfaceConfiguration, SurfaceError};

use winit::event;
use winit::{event_loop::EventLoopWindowTarget, window::Window};

use crate::RenderPipelineInfo;

// struct WindowRenderPipeline<'a> {
//     pipeline: RenderPipeline,
//     input_layout: VertexBufferLayout<'a>,
// }

pub trait LayoutedVertex {
    fn get_layout() -> wgpu::VertexBufferLayout<'static>;
}

pub struct HostScreen {
    window: Window,
    surface: Surface,
    surface_config: SurfaceConfiguration,
    device: Device,
    queue: Queue,
    // command_encoder: CommandEncoder,
    pipelines: Vec<Box<RenderPipeline>>,
    // on_pipeline_render: Box<dyn Fn(usize)>,
}

impl HostScreen {
    // pub fn get_window(&self) -> &Window {
    //     &self.window
    // }
    // pub fn get_instance(&self) -> &Instance {
    //     &self._instance
    // }
    // pub fn get_surface(&self) -> &Surface {
    //     &self.surface
    // }
    // pub fn get_surface_config(&self) -> &SurfaceConfiguration {
    //     &self.surface_config
    // }
    // pub fn get_device(&self) -> &Device {
    //     &self.device
    // }
    // pub fn get_queue(&self) -> &Queue {
    //     &self.queue
    // }

    const CLEAR_COLOR: wgpu::Color = wgpu::Color {
        r: 0.0,
        g: 0.0,
        b: 0.0,
        a: 0.0,
    };

    // pub fn window_id(&self) -> WindowId {
    //     self.window.id()
    // }

    // fn create_default_pipeline(
    //     device: &Device,
    //     shader: ShaderModule,
    //     input_vertex_layout: VertexBufferLayout,
    //     output_format: &TextureFormat,
    // ) -> wgpu::RenderPipeline {
    //     let render_pipeline_layout =
    //         device.create_pipeline_layout(&wgpu::PipelineLayoutDescriptor {
    //             label: Some("Default Render Pipeline Layout"),
    //             bind_group_layouts: &[],
    //             push_constant_ranges: &[],
    //         });
    //     device.create_render_pipeline(&wgpu::RenderPipelineDescriptor {
    //         label: Some("Default Render Pipeline"),
    //         layout: Some(&render_pipeline_layout),
    //         vertex: wgpu::VertexState {
    //             module: &shader,
    //             entry_point: "vs_main",
    //             buffers: &[input_vertex_layout],
    //         },
    //         fragment: Some(wgpu::FragmentState {
    //             module: &shader,
    //             entry_point: "fs_main",
    //             targets: &[Some(wgpu::ColorTargetState {
    //                 format: *output_format,
    //                 blend: Some(wgpu::BlendState::REPLACE),
    //                 write_mask: wgpu::ColorWrites::ALL,
    //             })],
    //         }),
    //         primitive: wgpu::PrimitiveState {
    //             topology: wgpu::PrimitiveTopology::TriangleList,
    //             strip_index_format: None,
    //             front_face: wgpu::FrontFace::Ccw,
    //             cull_mode: Some(wgpu::Face::Back),
    //             polygon_mode: wgpu::PolygonMode::Fill,
    //             unclipped_depth: false,
    //             conservative: false,
    //         },
    //         depth_stencil: None,
    //         multisample: wgpu::MultisampleState {
    //             count: 1,
    //             mask: !0,
    //             alpha_to_coverage_enabled: false,
    //         },
    //         multiview: None,
    //     })
    // }

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

        // let shader = device.create_shader_module(wgpu::ShaderModuleDescriptor {
        //     label: Some("Shader"),
        //     source: wgpu::ShaderSource::Wgsl(include_str!("shader.wgsl").into()),
        // });

        // let pipeline =
        //     Self::create_default_pipeline(&device, shader, vertex_layout, &surface_config.format);
        // let command_encoder = device.create_command_encoder(&wgpu::CommandEncoderDescriptor {
        //     label: Some("Render Encoder"),
        // });
        Ok(HostScreen {
            window,
            surface_config,
            surface,
            device,
            queue,
            pipelines: vec![],
        })
    }

    // pub fn add_render_pipeline(&mut self, pipeline: RenderPipeline) {
    //     self.pipelines.push(Box::new(pipeline));
    // }
    pub fn add_render_pipeline(
        &mut self,
        rpi: &RenderPipelineInfo,
    ) -> Result<&RenderPipeline, Utf8Error> {
        let shader_source = std::str::from_utf8(rpi.shader_source)?;
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
                // let vertices = RenderTargetVertices {
                //     vertex_buffer: vertex_buffer.slice(..),
                //     vertices_range: 0..3,
                //     instances: 0..1,
                // };
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
            let mut _render_pass = command_encoder.begin_render_pass(&wgpu::RenderPassDescriptor {
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

            // let mut i = 0;
            // for pipeline in self.pipelines.iter() {
            //     render_pass.set_pipeline(pipeline);
            //     let on_pipeline_render = self.on_pipeline_render.as_ref();
            //     on_pipeline_render(i);
            //     i += 1;
            //     render_pass.set_vertex_buffer(0, vertices.vertex_buffer);
            //     render_pass.draw(vertices.vertices_range.clone(), vertices.instances.clone());
            // }
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

// pub struct RenderTargetVertices<'a> {
//     pub vertex_buffer: BufferSlice<'a>,
//     pub vertices_range: Range<u32>,
//     pub instances: Range<u32>,
// }
