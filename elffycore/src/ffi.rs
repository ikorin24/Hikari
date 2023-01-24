use crate::engine::*;
use crate::error::*;
use crate::traceln;
use crate::types::*;
use std::num::{NonZeroU32, NonZeroUsize};

#[no_mangle]
extern "cdecl" fn elffy_engine_start(
    engine_config: &EngineCoreConfig,
    screen_config: &HostScreenConfig,
) -> NonZeroUsize {
    let err = engine_start(engine_config, screen_config);
    dispatch_err(err);
    let err_count = reset_tls_err_count();
    NonZeroUsize::new(err_count).unwrap()
}

#[no_mangle]
extern "cdecl" fn elffy_screen_set_inner_size(
    screen: &HostScreen,
    width: u32,
    height: u32,
) -> ApiResult {
    if let (Some(w), Some(h)) = (NonZeroU32::new(width), NonZeroU32::new(height)) {
        screen.set_inner_size(w, h);
    }
    make_result()
}

#[no_mangle]
extern "cdecl" fn elffy_screen_get_inner_size(
    screen: &HostScreen,
    width: &mut u32,
    height: &mut u32,
) -> ApiResult {
    (*width, *height) = screen.get_inner_size();
    make_result()
}

#[no_mangle]
extern "cdecl" fn elffy_write_texture(
    screen: &HostScreen,
    texture: &ImageCopyTexture,
    data: Slice<u8>,
    data_layout: &wgpu::ImageDataLayout,
    size: &wgpu::Extent3d,
) -> ApiResult {
    screen.write_texture(texture, &data, data_layout, size);
    make_result()
}

#[no_mangle]
extern "cdecl" fn elffy_create_bind_group_layout(
    screen: &HostScreen,
    desc: &BindGroupLayoutDescriptor,
) -> ApiRefResult<wgpu::BindGroupLayout> {
    let value = desc.use_wgpu_type(|x| screen.create_bind_group_layout(x));
    make_ref_result(value, None)
}

#[no_mangle]
extern "cdecl" fn elffy_destroy_bind_group_layout(layout: Box<wgpu::BindGroupLayout>) {
    drop(layout)
}

#[no_mangle]
extern "cdecl" fn elffy_create_bind_group(
    screen: &HostScreen,
    desc: &BindGroupDescriptor,
) -> ApiRefResult<wgpu::BindGroup> {
    let value = desc.use_wgpu_type(|x| screen.create_bind_group(x));
    make_ref_result(value, None)
}

#[no_mangle]
extern "cdecl" fn elffy_destroy_bind_group(bind_group: Box<wgpu::BindGroup>) {
    drop(bind_group)
}

#[no_mangle]
extern "cdecl" fn elffy_create_pipeline_layout(
    screen: &HostScreen,
    desc: &PipelineLayoutDescriptor,
) -> ApiRefResult<wgpu::PipelineLayout> {
    let value = screen.create_pipeline_layout(&desc.to_pipeline_descriptor());
    make_ref_result(value, None)
}

#[no_mangle]
extern "cdecl" fn elffy_destroy_pipeline_layout(layout: Box<wgpu::PipelineLayout>) {
    drop(layout)
}

#[no_mangle]
extern "cdecl" fn elffy_create_render_pipeline(
    screen: &HostScreen,
    desc: &RenderPipelineDescription,
) -> ApiRefResult<wgpu::RenderPipeline> {
    let value = match desc.use_wgpu_type(|x| Ok(screen.create_render_pipeline(x))) {
        Ok(value) => value,
        Err(err) => {
            return error_ref_result(err);
        }
    };
    make_ref_result(value, None)
}

#[no_mangle]
extern "cdecl" fn elffy_destroy_render_pipeline(pipeline: Box<wgpu::RenderPipeline>) {
    drop(pipeline)
}

#[no_mangle]
extern "cdecl" fn elffy_create_buffer_init(
    screen: &HostScreen,
    contents: Slice<u8>,
    usage: wgpu::BufferUsages,
) -> ApiRefResult<wgpu::Buffer> {
    let value = screen.create_buffer_init(&contents, usage);
    make_ref_result(value, Some(|value| value.destroy()))
}

#[no_mangle]
extern "cdecl" fn elffy_destroy_buffer(buffer: Box<wgpu::Buffer>) {
    buffer.destroy();
    drop(buffer)
}

#[no_mangle]
extern "cdecl" fn elffy_create_sampler(
    screen: &HostScreen,
    desc: &SamplerDescriptor,
) -> ApiRefResult<wgpu::Sampler> {
    let value = screen.create_sampler(&desc.to_wgpu_type());
    make_ref_result(value, None)
}

#[no_mangle]
extern "cdecl" fn elffy_destroy_sampler(sampler: Box<wgpu::Sampler>) {
    drop(sampler)
}

#[no_mangle]
extern "cdecl" fn elffy_create_shader_module(
    screen: &HostScreen,
    shader_source: Slice<u8>,
) -> ApiRefResult<wgpu::ShaderModule> {
    let shader_source = match shader_source.as_str() {
        Ok(s) => s,
        Err(err) => {
            return error_ref_result(err);
        }
    };
    let value = screen.create_shader_module(shader_source);
    make_ref_result(value, None)
}

#[no_mangle]
extern "cdecl" fn elffy_destroy_shader_module(shader: Box<wgpu::ShaderModule>) {
    drop(shader)
}

#[no_mangle]
extern "cdecl" fn elffy_create_texture(
    screen: &HostScreen,
    desc: &TextureDescriptor,
) -> ApiRefResult<wgpu::Texture> {
    let value = screen.create_texture(&desc.to_wgpu_type());
    make_ref_result(value, Some(|value| value.destroy()))
}

#[no_mangle]
extern "cdecl" fn elffy_create_texture_with_data(
    screen: &HostScreen,
    desc: &TextureDescriptor,
    data: Slice<u8>,
) -> ApiRefResult<wgpu::Texture> {
    let value = screen.create_texture_with_data(&desc.to_wgpu_type(), &data);
    make_ref_result(value, Some(|value| value.destroy()))
}

#[no_mangle]
extern "cdecl" fn elffy_destroy_texture(texture: Box<wgpu::Texture>) {
    texture.destroy();
    drop(texture)
}

#[no_mangle]
extern "cdecl" fn elffy_create_texture_view(
    texture: &wgpu::Texture,
    desc: &TextureViewDescriptor,
) -> ApiRefResult<wgpu::TextureView> {
    let desc = &desc.to_wgpu_type();
    traceln!(
        r"create_texture_view(
texture: {:#?},
desc: {:#?},
);",
        texture,
        desc,
    );
    let value = Box::new(texture.create_view(desc));
    make_ref_result(value, None)
}

#[no_mangle]
extern "cdecl" fn elffy_destroy_texture_view(texture_view: Box<wgpu::TextureView>) {
    drop(texture_view)
}

#[no_mangle]
extern "cdecl" fn elffy_write_buffer(
    screen: &HostScreen,
    buffer: &wgpu::Buffer,
    offset: u64,
    data: Slice<u8>,
) -> ApiResult {
    screen.write_buffer(buffer, offset, &data);
    make_result()
}

#[no_mangle]
extern "cdecl" fn elffy_set_pipeline<'a>(
    render_pass: &mut wgpu::RenderPass<'a>,
    render_pipeline: &'a wgpu::RenderPipeline,
) -> ApiResult {
    render_pass.set_pipeline(render_pipeline);
    make_result()
}

#[no_mangle]
extern "cdecl" fn elffy_set_bind_group<'a>(
    render_pass: &mut wgpu::RenderPass<'a>,
    index: u32,
    bind_group: &'a wgpu::BindGroup,
) -> ApiResult {
    render_pass.set_bind_group(index, bind_group, &[]);
    make_result()
}

#[no_mangle]
extern "cdecl" fn elffy_set_vertex_buffer<'a>(
    render_pass: &mut wgpu::RenderPass<'a>,
    slot: u32,
    buffer_slice: BufSlice<'a>,
) -> ApiResult {
    render_pass.set_vertex_buffer(slot, buffer_slice.to_buffer_slice());
    make_result()
}

#[no_mangle]
extern "cdecl" fn elffy_set_index_buffer<'a>(
    render_pass: &mut wgpu::RenderPass<'a>,
    buffer_slice: BufSlice<'a>,
    index_format: wgpu::IndexFormat,
) -> ApiResult {
    render_pass.set_index_buffer(buffer_slice.to_buffer_slice(), index_format);
    make_result()
}

#[no_mangle]
extern "cdecl" fn elffy_draw<'a>(
    render_pass: &mut wgpu::RenderPass<'a>,
    vertices: RangeU32,
    instances: RangeU32,
) -> ApiResult {
    render_pass.draw(vertices.to_range(), instances.to_range());
    make_result()
}

#[no_mangle]
extern "cdecl" fn elffy_draw_indexed<'a>(
    render_pass: &mut wgpu::RenderPass<'a>,
    indices: RangeU32,
    base_vertex: i32,
    instances: RangeU32,
) -> ApiResult {
    render_pass.draw_indexed(indices.to_range(), base_vertex, instances.to_range());
    make_result()
}

#[inline]
fn make_ref_result<T>(value: Box<T>, on_value_drop: Option<fn(Box<T>)>) -> ApiRefResult<T> {
    let err_count = reset_tls_err_count();
    match NonZeroUsize::new(err_count) {
        Some(err_count) => {
            if let Some(on_value_drop) = on_value_drop {
                on_value_drop(value);
            }
            ApiRefResult::err(err_count)
        }
        None => ApiRefResult::ok(value),
    }
}

#[inline]
fn error_ref_result<T>(err: impl std::fmt::Display) -> ApiRefResult<T> {
    dispatch_err(err);
    return ApiRefResult::err(reset_tls_err_count().try_into().unwrap());
}

#[inline]
fn make_result() -> ApiResult {
    let err_count = reset_tls_err_count();
    ApiResult::from_err_count(err_count)
}
