use crate::engine::*;
use crate::types::*;

#[no_mangle]
extern "cdecl" fn elffy_engine_start(init: HostScreenInitFn) {
    engine_start(init);
}

#[no_mangle]
extern "cdecl" fn elffy_add_render_pipeline(
    screen: &mut HostScreen,
    render_pipeline: &RenderPipelineInfo,
) -> *const wgpu::RenderPipeline {
    match screen.add_render_pipeline(render_pipeline) {
        Ok(render_pipeline) => render_pipeline,
        Err(err) => {
            panic!("{:?}", err)
        }
    }
}

#[no_mangle]
extern "cdecl" fn elffy_create_buffer_init(
    screen: &mut HostScreen,
    contents: Sliceffi<u8>,
    usage: wgpu::BufferUsages,
) -> &wgpu::Buffer {
    screen.create_buffer_init(contents.as_slice(), usage)
}

#[no_mangle]
extern "cdecl" fn elffy_set_pipeline<'a>(
    render_pass: &'a mut wgpu::RenderPass<'a>,
    render_pipeline: &'a wgpu::RenderPipeline,
) {
    render_pass.set_pipeline(render_pipeline);
}

#[no_mangle]
extern "cdecl" fn elffy_draw<'a>(
    render_pass: &'a mut wgpu::RenderPass<'a>,
    slot: u32,
    buffer_slice: &'a BufferSliceffi,
    vertices_range: &'a RangeU32ffi,
    instances_range: &'a RangeU32ffi,
) {
    render_pass.set_vertex_buffer(slot, buffer_slice.to_buffer_slice());
    render_pass.draw(vertices_range.to_range(), instances_range.to_range());
}
