use crate::engine::*;
use crate::screen::*;
use crate::*;
use std::num::{NonZeroU32, NonZeroUsize};

/// # Thread Safety
/// Only from main thread.
/// (I do not know if it is thread safe or not. But that's good enough for me.)
/// ## NG
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn elffy_engine_start(
    engine_config: &EngineCoreConfig,
    screen_config: &HostScreenConfig,
) -> ApiResult {
    if let Err(err) = engine_start(engine_config, screen_config) {
        Engine::dispatch_err(err);
    }
    make_result()
}

static_assertions::assert_impl_all!(HostScreenConfig: Send, Sync);

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn elffy_create_screen(config: &HostScreenConfig) -> ApiResult {
    match send_proxy_message(ProxyMessage::CreateScreen(*config)) {
        Ok(_) => {}
        Err(err) => Engine::dispatch_err(err),
    };
    make_result()
}

static_assertions::assert_impl_all!(HostScreen: Send, Sync);
static_assertions::assert_impl_all!(Slice<u8>: Send, Sync);

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn elffy_screen_resize_surface(
    screen: &HostScreen,
    width: u32,
    height: u32,
) -> ApiResult {
    screen.resize_surface(width, height);
    make_result()
}

/// # Thread Safety
/// Only from main thread. (iOS requires that.)
/// ## NG
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn elffy_screen_request_redraw(screen: &HostScreen) -> ApiResult {
    screen.window.request_redraw();
    make_result()
}

/// # Thread Safety
/// Only from main thread.
/// (I do not know if it is thread safe or not. But that's good enough for me.)
/// ## NG
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn elffy_screen_begin_command(
    screen: &HostScreen,
) -> ApiValueResult<BeginCommandData> {
    match screen.surface.get_current_texture() {
        Ok(surface_texture) => {
            let view = surface_texture
                .texture
                .create_view(&wgpu::TextureViewDescriptor::default());
            let command_encoder = screen
                .device
                .create_command_encoder(&wgpu::CommandEncoderDescriptor { label: None });
            let value = BeginCommandData::new(
                Box::new(command_encoder),
                Box::new(surface_texture),
                Box::new(view),
            );
            make_value_result(value)
        }
        Err(wgpu::SurfaceError::Lost) => {
            let size = screen.window.inner_size();
            screen.resize_surface(size.width, size.height);
            let value = BeginCommandData::failed();
            make_value_result(value)
        }
        Err(err) => error_value_result(err),
    }
}

/// # Thread Safety
/// Only from main thread.
/// (I do not know if it is thread safe or not. But that's good enough for me.)
/// ## NG
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn elffy_screen_finish_command(
    screen: &HostScreen,
    command_encoder: Box<wgpu::CommandEncoder>,
    surface_tex: Box<wgpu::SurfaceTexture>,
    surface_tex_view: Box<wgpu::TextureView>,
) -> ApiResult {
    let command_encoder = *command_encoder;
    screen
        .queue
        .submit(std::iter::once(command_encoder.finish()));
    surface_tex.present();
    drop(surface_tex_view);
    make_result()
}

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn elffy_screen_set_title(screen: &HostScreen, title: Slice<u8>) -> ApiResult {
    match title.as_str() {
        Ok(title) => {
            screen.window.set_title(title);
        }
        Err(err) => {
            Engine::dispatch_err(err);
        }
    }
    make_result()
}

/// # Thread Safety
/// Only from main thread.
/// (I do not know if it is thread safe or not. But that's good enough for me.)
/// ## NG
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn elffy_create_render_pass<'tex, 'desc, 'cmd_enc>(
    command_encoder: &'cmd_enc mut wgpu::CommandEncoder,
    desc: &'desc RenderPassDescriptor<'tex, 'desc>,
) -> ApiBoxResult<wgpu::RenderPass<'cmd_enc>>
where
    'tex: 'cmd_enc,
{
    let render_pass = desc.begin_render_pass_with(command_encoder);
    // `command_encoder` is no longer accessible until `render_pass` will drop.

    make_box_result(Box::new(render_pass))
}

static_assertions::assert_impl_all!(Box<wgpu::RenderPass>: Send, Sync);
static_assertions::assert_impl_all!(wgpu::RenderPass: Send, Sync);

/// Destroy [`Box<wgpu::RenderPass>`].
///
/// # Thread Safety
/// ## OK
/// - called from any thread
/// ## NG
/// - called from multiple threads simultaneously with same args with same args
#[no_mangle]
extern "cdecl" fn elffy_destroy_render_pass<'cmd_enc>(
    render_pass: Box<wgpu::RenderPass<'cmd_enc>>,
) {
    drop(render_pass)
}

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
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

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn elffy_screen_get_inner_size(screen: &HostScreen) -> ApiValueResult<SizeU32> {
    let size: (u32, u32) = screen.window.inner_size().into();
    make_value_result(size.into())
}

#[no_mangle]
extern "cdecl" fn elffy_screen_set_location(
    screen: &HostScreen,
    x: i32,
    y: i32,
    monitor_id: Opt<MonitorId>,
) -> ApiResult {
    let f = || -> Result<_, Box<dyn Error>> {
        let window = &screen.window;
        let monitor = match monitor_id.to_option() {
            Some(id) => id.monitor(),
            None => window.current_monitor().ok_or("no monitors")?,
        };
        let offset = monitor.position();
        let p: winit::dpi::PhysicalPosition<i32> = (offset.x + x, offset.y + y).into();
        window.set_outer_position(p);
        Ok(())
    };
    match f() {
        Ok(_) => {}
        Err(err) => Engine::dispatch_err(err),
    };
    make_result()
}

/// # Thread Safety
/// (iOS) Only from main thread.
/// (`winit::monitor::MonitorHandle` can only be used on the main thread in iOS.)
/// ## NG
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn elffy_screen_get_location(
    screen: &HostScreen,
    monitor_id: Opt<MonitorId>,
) -> ApiValueResult<Tuple<i32, i32>> {
    let f = || -> Result<_, Box<dyn Error>> {
        let window = &screen.window;
        let monitor = match monitor_id.to_option() {
            Some(id) => id.monitor(),
            None => window.current_monitor().ok_or("no monitors")?,
        };
        let offset = monitor.position();
        let p = window.outer_position()?;
        Ok((p.x - offset.x, p.y - offset.y))
    };

    match f() {
        Ok(location) => make_value_result(location.into()),
        Err(err) => error_value_result(err),
    }
}

/// # Thread Safety
/// (iOS) Only from main thread.
/// (`Window::current_monitor` can only be called on the main thread in iOS.)
/// ## NG
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn elffy_current_monitor(screen: &HostScreen) -> ApiValueResult<Opt<MonitorId>> {
    match screen.window.current_monitor().ok_or("no monitors") {
        Ok(monitor) => make_value_result(Some(MonitorId::new(monitor)).into()),
        Err(err) => error_value_result(err),
    }
}

/// # Thread Safety
/// (iOS) Only from main thread.
/// (`Window::available_monitors` can only be called on the main thread in iOS.)
/// ## NG
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn elffy_monitor_count(screen: &HostScreen) -> ApiValueResult<usize> {
    let count = screen.window.available_monitors().count();
    make_value_result(count)
}

/// # Thread Safety
/// (iOS) Only from main thread.
/// (`Window::available_monitors` can only be called on the main thread in iOS.)
/// ## NG
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn elffy_monitors(
    screen: &HostScreen,
    buf: *mut MonitorId,
    buflen: usize,
) -> ApiValueResult<usize> {
    let mut i: usize = 0;
    let buf = unsafe { std::slice::from_raw_parts_mut(buf, buflen) };
    for monitor in screen.window.available_monitors().take(buflen) {
        buf[i] = MonitorId::new(monitor);
        i += 1;
    }
    make_value_result(i)
}

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn elffy_write_texture(
    screen: &HostScreen,
    texture: &ImageCopyTexture,
    data: Slice<u8>,
    data_layout: &wgpu::ImageDataLayout,
    size: &wgpu::Extent3d,
) -> ApiResult {
    screen
        .queue
        .write_texture(texture.to_wgpu_type(), &data, *data_layout, *size);
    make_result()
}

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn elffy_create_bind_group_layout(
    screen: &HostScreen,
    desc: &BindGroupLayoutDescriptor,
) -> ApiBoxResult<wgpu::BindGroupLayout> {
    let value = desc.use_wgpu_type(|desc| {
        let layout = screen.device.create_bind_group_layout(desc);
        Box::new(layout)
    });
    make_box_result(value)
}

static_assertions::assert_impl_all!(Box<wgpu::BindGroupLayout>: Send, Sync);
static_assertions::assert_impl_all!(wgpu::BindGroupLayout: Send, Sync);

/// Destroy [`Box<wgpu::BindGroupLayout>`].
///
/// # Thread Safety
/// ## OK
/// - called from any thread
/// ## NG
/// - called from multiple threads simultaneously with same args with same args
#[no_mangle]
extern "cdecl" fn elffy_destroy_bind_group_layout(layout: Box<wgpu::BindGroupLayout>) {
    drop(layout)
}

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn elffy_create_bind_group(
    screen: &HostScreen,
    desc: &BindGroupDescriptor,
) -> ApiBoxResult<wgpu::BindGroup> {
    let value = desc.use_wgpu_type(|desc| {
        let bind_group = screen.device.create_bind_group(desc);
        Box::new(bind_group)
    });
    make_box_result(value)
}

static_assertions::assert_impl_all!(Box<wgpu::BindGroup>: Send, Sync);
static_assertions::assert_impl_all!(wgpu::BindGroup: Send, Sync);

/// Destroy [`Box<wgpu::BindGroup>`].
///
/// # Thread Safety
/// ## OK
/// - called from any thread
/// ## NG
/// - called from multiple threads simultaneously with same args with same args
#[no_mangle]
extern "cdecl" fn elffy_destroy_bind_group(bind_group: Box<wgpu::BindGroup>) {
    drop(bind_group)
}

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn elffy_create_pipeline_layout(
    screen: &HostScreen,
    desc: &PipelineLayoutDescriptor,
) -> ApiBoxResult<wgpu::PipelineLayout> {
    let value = screen.device.create_pipeline_layout(&desc.to_wgpu_type());
    let value = Box::new(value);
    make_box_result(value)
}

static_assertions::assert_impl_all!(Box<wgpu::PipelineLayout>: Send, Sync);
static_assertions::assert_impl_all!(wgpu::PipelineLayout: Send, Sync);

/// Destroy [`Box<wgpu::PipelineLayout>`].
///
/// # Thread Safety
/// ## OK
/// - called from any thread
/// ## NG
/// - called from multiple threads simultaneously with same args with same args
#[no_mangle]
extern "cdecl" fn elffy_destroy_pipeline_layout(layout: Box<wgpu::PipelineLayout>) {
    drop(layout)
}

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn elffy_create_render_pipeline(
    screen: &HostScreen,
    desc: &RenderPipelineDescriptor,
) -> ApiBoxResult<wgpu::RenderPipeline> {
    let value = match desc.use_wgpu_type(|desc| {
        let value = screen.device.create_render_pipeline(desc);
        Ok(Box::new(value))
    }) {
        Ok(value) => value,
        Err(err) => {
            return error_box_result(err);
        }
    };
    make_box_result(value)
}

static_assertions::assert_impl_all!(Box<wgpu::RenderPipeline>: Send, Sync);
static_assertions::assert_impl_all!(wgpu::RenderPipeline: Send, Sync);

/// Destroy [`Box<wgpu::RenderPipeline>`].
///
/// # Thread Safety
/// ## OK
/// - called from any thread
/// ## NG
/// - called from multiple threads simultaneously with same args with same args
#[no_mangle]
extern "cdecl" fn elffy_destroy_render_pipeline(pipeline: Box<wgpu::RenderPipeline>) {
    drop(pipeline)
}

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn elffy_create_buffer_init(
    screen: &HostScreen,
    contents: Slice<u8>,
    usage: wgpu::BufferUsages,
) -> ApiBoxResult<wgpu::Buffer> {
    use wgpu::util::DeviceExt;

    let buffer = screen
        .device
        .create_buffer_init(&wgpu::util::BufferInitDescriptor {
            label: None,
            contents: &contents,
            usage,
        });
    let value = Box::new(buffer);
    make_box_result(value)
}

static_assertions::assert_impl_all!(Box<wgpu::Buffer>: Send, Sync);
static_assertions::assert_impl_all!(wgpu::Buffer: Send, Sync);

/// Destroy [`Box<wgpu::Buffer>`].
///
/// # Thread Safety
/// ## OK
/// - called from any thread
/// ## NG
/// - called from multiple threads simultaneously with same args with same args
#[no_mangle]
extern "cdecl" fn elffy_destroy_buffer(buffer: Box<wgpu::Buffer>) {
    drop(buffer)
}

static_assertions::assert_impl_all!(SamplerDescriptor: Send, Sync);

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn elffy_create_sampler(
    screen: &HostScreen,
    desc: &SamplerDescriptor,
) -> ApiBoxResult<wgpu::Sampler> {
    let sampler = screen.device.create_sampler(&desc.to_wgpu_type());
    let value = Box::new(sampler);
    make_box_result(value)
}

static_assertions::assert_impl_all!(Box<wgpu::Sampler>: Send, Sync);
static_assertions::assert_impl_all!(wgpu::Sampler: Send, Sync);

/// Destroy [`Box<wgpu::Sampler>`].
///
/// # Thread Safety
/// ## OK
/// - called from any thread
/// ## NG
/// - called from multiple threads simultaneously with same args with same args
#[no_mangle]
extern "cdecl" fn elffy_destroy_sampler(sampler: Box<wgpu::Sampler>) {
    drop(sampler)
}

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn elffy_create_shader_module(
    screen: &HostScreen,
    shader_source: Slice<u8>,
) -> ApiBoxResult<wgpu::ShaderModule> {
    let shader_source = match shader_source.as_str() {
        Ok(s) => s,
        Err(err) => {
            return error_box_result(err);
        }
    };
    let shader = screen
        .device
        .create_shader_module(wgpu::ShaderModuleDescriptor {
            label: None,
            source: wgpu::ShaderSource::Wgsl(shader_source.into()),
        });
    let value = Box::new(shader);
    make_box_result(value)
}

static_assertions::assert_impl_all!(Box<wgpu::ShaderModule>: Send, Sync);
static_assertions::assert_impl_all!(wgpu::ShaderModule: Send, Sync);

/// Destroy [`Box<wgpu::ShaderModule>`].
///
/// # Thread Safety
/// ## OK
/// - called from any thread
/// ## NG
/// - called from multiple threads simultaneously with same args with same args
#[no_mangle]
extern "cdecl" fn elffy_destroy_shader_module(shader: Box<wgpu::ShaderModule>) {
    drop(shader)
}

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn elffy_create_texture(
    screen: &HostScreen,
    desc: &TextureDescriptor,
) -> ApiBoxResult<wgpu::Texture> {
    let value = Box::new(screen.device.create_texture(&desc.to_wgpu_type()));
    make_box_result(value)
}

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn elffy_create_texture_with_data(
    screen: &HostScreen,
    desc: &TextureDescriptor,
    data: Slice<u8>,
) -> ApiBoxResult<wgpu::Texture> {
    use wgpu::util::DeviceExt;

    let texture =
        screen
            .device
            .create_texture_with_data(&screen.queue, &desc.to_wgpu_type(), &data);
    let value = Box::new(texture);
    make_box_result(value)
}

static_assertions::assert_impl_all!(Box<wgpu::Texture>: Send, Sync);
static_assertions::assert_impl_all!(wgpu::Texture: Send, Sync);

/// Destroy [`Box<wgpu::Texture>`].
///
/// # Thread Safety
/// ## OK
/// - called from any thread
/// ## NG
/// - called from multiple threads simultaneously with same args with same args
#[no_mangle]
extern "cdecl" fn elffy_destroy_texture(texture: Box<wgpu::Texture>) {
    drop(texture)
}

/// # Thread Safety
/// ## OK
/// - called from any thread
/// ## NG
/// - called from multiple threads simultaneously with same args with same args
/// (`info_out` is mutable reference)
#[no_mangle]
extern "cdecl" fn elffy_texture_format_info(
    format: TextureFormat,
    info_out: &mut TextureFormatInfo,
) -> ApiResult {
    *info_out = format.to_wgpu_type().describe().into();
    make_result()
}

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn elffy_create_texture_view(
    texture: &wgpu::Texture,
    desc: &TextureViewDescriptor,
) -> ApiBoxResult<wgpu::TextureView> {
    let desc = &desc.to_wgpu_type();
    let value = Box::new(texture.create_view(desc));
    make_box_result(value)
}

static_assertions::assert_impl_all!(Box<wgpu::TextureView>: Send, Sync);
static_assertions::assert_impl_all!(wgpu::TextureView: Send, Sync);

/// Destroy [`Box<wgpu::TextureView>`].
///
/// # Thread Safety
/// ## OK
/// - called from any thread
/// ## NG
/// - called from multiple threads simultaneously with same args with same args
#[no_mangle]
extern "cdecl" fn elffy_destroy_texture_view(texture_view: Box<wgpu::TextureView>) {
    drop(texture_view)
}

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn elffy_write_buffer(
    screen: &HostScreen,
    buffer: &wgpu::Buffer,
    offset: u64,
    data: Slice<u8>,
) -> ApiResult {
    screen.queue.write_buffer(buffer, offset, &data);
    make_result()
}

static_assertions::assert_impl_all!(wgpu::RenderPass: Send, Sync);
static_assertions::assert_impl_all!(wgpu::RenderPipeline: Send, Sync);

/// # Thread Safety
/// It cannot be called at the same time as other functions that use same `&mut wgpu::RenderPass`.
/// Multiple mutable references cannot exist simultaneously.
/// ## OK
/// - called from any thread
/// ## NG
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn elffy_set_pipeline<'a>(
    render_pass: &mut wgpu::RenderPass<'a>,
    render_pipeline: &'a wgpu::RenderPipeline,
) -> ApiResult {
    render_pass.set_pipeline(render_pipeline);
    make_result()
}

static_assertions::assert_impl_all!(wgpu::RenderPass: Send, Sync);
static_assertions::assert_impl_all!(wgpu::BindGroup: Send, Sync);

/// # Thread Safety
/// It cannot be called at the same time as other functions that use same `&mut wgpu::RenderPass`.
/// Multiple mutable references cannot exist simultaneously.
/// ## OK
/// - called from any thread
/// ## NG
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn elffy_set_bind_group<'a>(
    render_pass: &mut wgpu::RenderPass<'a>,
    index: u32,
    bind_group: &'a wgpu::BindGroup,
) -> ApiResult {
    render_pass.set_bind_group(index, bind_group, &[]);
    make_result()
}

static_assertions::assert_impl_all!(wgpu::RenderPass: Send, Sync);
static_assertions::assert_impl_all!(BufferSlice: Send, Sync);

/// # Thread Safety
/// It cannot be called at the same time as other functions that use same `&mut wgpu::RenderPass`.
/// Multiple mutable references cannot exist simultaneously.
/// ## OK
/// - called from any thread
/// ## NG
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn elffy_set_vertex_buffer<'a>(
    render_pass: &mut wgpu::RenderPass<'a>,
    slot: u32,
    buffer_slice: BufferSlice<'a>,
) -> ApiResult {
    render_pass.set_vertex_buffer(slot, buffer_slice.to_wgpu_type());
    make_result()
}

static_assertions::assert_impl_all!(wgpu::RenderPass: Send, Sync);
static_assertions::assert_impl_all!(BufferSlice: Send, Sync);
static_assertions::assert_impl_all!(wgpu::IndexFormat: Send, Sync);

/// # Thread Safety
/// It cannot be called at the same time as other functions that use same `&mut wgpu::RenderPass`.
/// Multiple mutable references cannot exist simultaneously.
/// ## OK
/// - called from any thread
/// ## NG
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn elffy_set_index_buffer<'a>(
    render_pass: &mut wgpu::RenderPass<'a>,
    buffer_slice: BufferSlice<'a>,
    index_format: wgpu::IndexFormat,
) -> ApiResult {
    render_pass.set_index_buffer(buffer_slice.to_wgpu_type(), index_format);
    make_result()
}

static_assertions::assert_impl_all!(wgpu::RenderPass: Send, Sync);
static_assertions::assert_impl_all!(RangeU32: Send, Sync);

/// # Thread Safety
/// It cannot be called at the same time as other functions that use same `&mut wgpu::RenderPass`.
/// Multiple mutable references cannot exist simultaneously.
/// ## OK
/// - called from any thread
/// ## NG
/// - called from multiple threads simultaneously with same argss
#[no_mangle]
extern "cdecl" fn elffy_draw<'a>(
    render_pass: &mut wgpu::RenderPass<'a>,
    vertices: RangeU32,
    instances: RangeU32,
) -> ApiResult {
    render_pass.draw(vertices.to_range(), instances.to_range());
    make_result()
}

static_assertions::assert_impl_all!(wgpu::RenderPass: Send, Sync);
static_assertions::assert_impl_all!(RangeU32: Send, Sync);
static_assertions::assert_impl_all!(i32: Send, Sync);

/// # Thread Safety
/// It cannot be called at the same time as other functions that use same `&mut wgpu::RenderPass`.
/// Multiple mutable references cannot exist simultaneously.
/// ## OK
/// - called from any thread
/// ## NG
/// - called from multiple threads simultaneously with same argss
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

static_assertions::assert_impl_all!(winit::window::Window: Send, Sync);

#[no_mangle]
extern "cdecl" fn elffy_set_ime_allowed(screen: &HostScreen, allowed: bool) -> ApiResult {
    screen.window.set_ime_allowed(allowed);
    make_result()
}

#[no_mangle]
extern "cdecl" fn elffy_set_ime_position(screen: &HostScreen, x: u32, y: u32) -> ApiResult {
    let pos = winit::dpi::PhysicalPosition::new(x, y);
    screen.window.set_ime_position(pos);
    make_result()
}

#[inline]
fn make_box_result<T>(value: Box<T>) -> ApiBoxResult<T> {
    let err_count = reset_tls_err_count();
    match NonZeroUsize::new(err_count) {
        Some(err_count) => ApiBoxResult::err(err_count),
        None => ApiBoxResult::ok(value),
    }
}

#[inline]
fn error_box_result<T>(err: impl std::fmt::Display) -> ApiBoxResult<T> {
    Engine::dispatch_err(err);
    let err_count = reset_tls_err_count().try_into().unwrap();
    ApiBoxResult::err(err_count)
}

#[inline]
fn make_value_result<T: Default + 'static>(value: T) -> ApiValueResult<T> {
    let err_count = reset_tls_err_count();
    match NonZeroUsize::new(err_count) {
        Some(err_count) => ApiValueResult::err(err_count),
        None => ApiValueResult::ok(value),
    }
}

#[inline]
fn error_value_result<T: Default + 'static>(err: impl std::fmt::Display) -> ApiValueResult<T> {
    Engine::dispatch_err(err);
    let err_count = reset_tls_err_count().try_into().unwrap();
    ApiValueResult::err(err_count)
}

#[inline]
fn make_result() -> ApiResult {
    let err_count = reset_tls_err_count();
    ApiResult::from_err_count(err_count)
}
