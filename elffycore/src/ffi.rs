use crate::engine::*;
use crate::types::*;
use std::ops;

#[no_mangle]
extern "cdecl" fn elffy_engine_start(
    engine_config: &EngineCoreConfig,
    screen_config: &HostScreenConfig,
) {
    engine_start(engine_config, screen_config);
}

#[no_mangle]
extern "cdecl" fn elffy_create_bind_group_layout<'screen>(
    screen: &'screen mut HostScreen,
    desc: &BindGroupLayoutDescriptor,
) -> &'screen wgpu::BindGroupLayout {
    desc.use_wgpu_type(|x| screen.create_bind_group_layout(x))
}

#[no_mangle]
extern "cdecl" fn elffy_destroy_bind_group_layout<'screen>(
    screen: &'screen mut HostScreen,
    layout: &wgpu::BindGroupLayout,
) -> bool {
    screen.destroy_bind_group_layout(layout)
}

#[no_mangle]
extern "cdecl" fn elffy_create_bind_group<'screen>(
    screen: &'screen mut HostScreen,
    desc: &BindGroupDescriptor,
) -> &'screen wgpu::BindGroup {
    desc.use_wgpu_type(|x| screen.create_bind_group(x))
}

#[no_mangle]
extern "cdecl" fn elffy_destroy_bind_group(
    screen: &mut HostScreen,
    bind_group: &wgpu::BindGroup,
) -> bool {
    screen.destroy_bind_group(bind_group)
}

#[no_mangle]
extern "cdecl" fn elffy_create_pipeline_layout<'screen>(
    screen: &'screen mut HostScreen,
    desc: &PipelineLayoutDescriptor,
) -> &'screen wgpu::PipelineLayout {
    screen.create_pipeline_layout(&desc.to_pipeline_descriptor())
}

#[no_mangle]
extern "cdecl" fn elffy_destroy_pipeline_layout(
    screen: &mut HostScreen,
    layout: &wgpu::PipelineLayout,
) -> bool {
    screen.destroy_pipeline_layout(layout)
}

#[no_mangle]
extern "cdecl" fn elffy_create_render_pipeline<'screen>(
    screen: &'screen mut HostScreen,
    desc: &RenderPipelineDescription,
) -> &'screen wgpu::RenderPipeline {
    desc.use_wgpu_type(|x| screen.create_render_pipeline(x))
}

#[no_mangle]
extern "cdecl" fn elffy_destroy_render_pipeline(
    screen: &mut HostScreen,
    pipeline: &wgpu::RenderPipeline,
) -> bool {
    screen.destroy_render_pipeline(pipeline)
}

#[no_mangle]
extern "cdecl" fn elffy_create_buffer_init<'screen>(
    screen: &'screen mut HostScreen,
    contents: Slice<'screen, u8>,
    usage: wgpu::BufferUsages,
) -> &'screen wgpu::Buffer {
    screen.create_buffer_init(&contents, usage)
}

#[no_mangle]
extern "cdecl" fn elffy_destroy_buffer(screen: &mut HostScreen, buffer: &wgpu::Buffer) -> bool {
    screen.destroy_buffer(buffer)
}

#[no_mangle]
extern "cdecl" fn elffy_create_sampler<'screen>(
    screen: &'screen mut HostScreen,
    desc: &SamplerDescriptor,
) -> &'screen wgpu::Sampler {
    screen.create_sampler(&desc.to_wgpu_type())
}

#[no_mangle]
extern "cdecl" fn elffy_destroy_sampler(screen: &mut HostScreen, sampler: &wgpu::Sampler) -> bool {
    screen.destroy_sampler(sampler)
}

#[no_mangle]
extern "cdecl" fn elffy_create_shader_module<'screen>(
    screen: &'screen mut HostScreen,
    shader_source: Slice<u8>,
) -> &'screen wgpu::ShaderModule {
    let shader_source = shader_source
        .as_str()
        .unwrap_or_else(|err| panic!("{:?}", err));
    screen.create_shader_module(shader_source)
}

#[no_mangle]
extern "cdecl" fn elffy_destroy_shader_module(
    screen: &mut HostScreen,
    shader: &wgpu::ShaderModule,
) -> bool {
    screen.destroy_shader_module(shader)
}

#[no_mangle]
extern "cdecl" fn elffy_create_texture<'screen>(
    screen: &'screen mut HostScreen,
    desc: &TextureDescriptor,
) -> &'screen wgpu::Texture {
    screen.create_texture(&desc.to_wgpu_type())
}

#[no_mangle]
extern "cdecl" fn elffy_create_texture_with_data<'screen>(
    screen: &'screen mut HostScreen,
    desc: &TextureDescriptor,
    data: Slice<u8>,
) -> &'screen wgpu::Texture {
    screen.create_texture_with_data(&desc.to_wgpu_type(), &data)
}

#[no_mangle]
extern "cdecl" fn elffy_destroy_texture(screen: &mut HostScreen, texture: &wgpu::Texture) -> bool {
    screen.destroy_texture(texture)
}

#[no_mangle]
extern "cdecl" fn elffy_create_texture_view<'screen>(
    screen: &'screen mut HostScreen,
    texture: &wgpu::Texture,
    desc: &TextureViewDescriptor,
) -> &'screen wgpu::TextureView {
    screen.create_texture_view(texture, &desc.to_wgpu_type())
}

#[no_mangle]
extern "cdecl" fn elffy_destroy_texture_view(
    screen: &mut HostScreen,
    texture_view: &wgpu::TextureView,
) -> bool {
    screen.destroy_texture_view(texture_view)
}

#[no_mangle]
extern "cdecl" fn elffy_set_pipeline<'a>(
    render_pass: &mut wgpu::RenderPass<'a>,
    render_pipeline: &'a wgpu::RenderPipeline,
) {
    render_pass.set_pipeline(render_pipeline);
}

#[no_mangle]
extern "cdecl" fn elffy_draw_buffer<'a>(
    render_pass: &mut wgpu::RenderPass<'a>,
    arg: &DrawBufferArg<'a>,
) {
    render_pass.set_vertex_buffer(
        arg.vertex_buffer.slot,
        arg.vertex_buffer.buffer_slice.to_buffer_slice(),
    );
    render_pass.draw(
        arg.vertices_range.to_range(),
        arg.instances_range.to_range(),
    );
}

#[no_mangle]
extern "cdecl" fn elffy_draw_buffer_indexed<'a>(
    render_pass: &'a mut wgpu::RenderPass<'a>,
    arg: &DrawBufferIndexedArg<'a>,
) {
    render_pass.set_vertex_buffer(arg.slot, arg.vertex_buffer_slice.to_buffer_slice());
    render_pass.set_index_buffer(arg.index_buffer_slice.to_buffer_slice(), arg.index_format);
    render_pass.draw_indexed(
        ops::Range {
            start: arg.index_start,
            end: arg.index_end_excluded,
        },
        0,
        ops::Range {
            start: arg.instance_start,
            end: arg.instance_end_excluded,
        },
    )
}

#[no_mangle]
extern "cdecl" fn elffy_draw_buffers_indexed<'a>(
    render_pass: &'a mut wgpu::RenderPass<'a>,
    arg: &DrawBuffersIndexedArg<'a>,
) {
    for vb in arg.vertex_buffers.iter() {
        render_pass.set_vertex_buffer(vb.slot, vb.buffer_slice.to_buffer_slice());
    }
    render_pass.set_index_buffer(arg.index_buffer_slice.to_buffer_slice(), arg.index_format);
    render_pass.draw_indexed(
        ops::Range {
            start: arg.index_start,
            end: arg.index_end_excluded,
        },
        0,
        ops::Range {
            start: arg.instance_start,
            end: arg.instance_end_excluded,
        },
    );
}
