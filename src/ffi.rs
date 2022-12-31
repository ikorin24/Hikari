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
extern "cdecl" fn elffy_create_buffer_init<'a>(
    screen: &'a mut HostScreen,
    contents: Sliceffi<'a, u8>,
    usage: wgpu::BufferUsages,
) -> &'a wgpu::Buffer {
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
extern "cdecl" fn elffy_draw_buffer<'a>(
    render_pass: &'a mut wgpu::RenderPass<'a>,
    vertex_buffer: &'a SlotBufferSliceffi,
    vertices_range: &'a RangeU32ffi,
    instances_range: &'a RangeU32ffi,
) {
    render_pass.set_vertex_buffer(
        vertex_buffer.slot,
        vertex_buffer.buffer_slice.to_buffer_slice(),
    );
    render_pass.draw(vertices_range.to_range(), instances_range.to_range());
}

#[no_mangle]
extern "cdecl" fn elffy_draw_buffer_indexed<'a>(
    render_pass: &'a mut wgpu::RenderPass<'a>,
    vertex_buffer: &'a SlotBufferSliceffi,
    index_buffer: &'a IndexBufferSliceffi,
    indices_range: &'a RangeU32ffi,
    instances_range: &'a RangeU32ffi,
) {
    render_pass.set_vertex_buffer(
        vertex_buffer.slot,
        vertex_buffer.buffer_slice.to_buffer_slice(),
    );
    render_pass.set_index_buffer(
        index_buffer.buffer_slice.to_buffer_slice(),
        index_buffer.format,
    );
    render_pass.draw_indexed(indices_range.to_range(), 0, instances_range.to_range());
}

#[no_mangle]
extern "cdecl" fn elffy_draw_buffers_indexed<'a>(
    render_pass: &'a mut wgpu::RenderPass<'a>,
    vertex_buffers: Sliceffi<SlotBufferSliceffi<'a>>,
    index_buffer: &'a IndexBufferSliceffi,
    indices_range: &'a RangeU32ffi,
    instances_range: &'a RangeU32ffi,
) {
    for vb in vertex_buffers.as_slice() {
        render_pass.set_vertex_buffer(vb.slot, vb.buffer_slice.to_buffer_slice());
    }
    render_pass.set_index_buffer(
        index_buffer.buffer_slice.to_buffer_slice(),
        index_buffer.format,
    );
    render_pass.draw_indexed(indices_range.to_range(), 0, instances_range.to_range());
}
