use crate::engine::*;
use crate::screen::*;
use crate::*;
use std::num::NonZeroU32;
use winit::dpi::PhysicalSize;

/// # Thread Safety
/// Only from main thread.
/// (I do not know if it is thread safe or not. But that's good enough for me.)
/// ## NG
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn hikari_engine_start(
    engine_config: &EngineCoreConfig,
    screen_config: &ScreenConfig,
) -> ApiResult {
    let result = engine_start(engine_config, screen_config);
    ApiResult::ok_or_set_error(result)
}

static_assertions::assert_impl_all!(ScreenConfig: Send, Sync);

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn hikari_create_screen(config: &ScreenConfig) -> ApiResult {
    let result = send_proxy_message(ProxyMessage::CreateScreen(*config));
    ApiResult::ok_or_set_error(result)
}

static_assertions::assert_impl_all!(Screen: Send, Sync);
static_assertions::assert_impl_all!(Slice<u8>: Send, Sync);

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn hikari_screen_resize_surface(
    screen: &Screen,
    width: u32,
    height: u32,
) -> ApiResult {
    screen.resize_surface(width, height);
    ApiResult::ok()
}

/// # Thread Safety
/// Only from main thread. (iOS requires that.)
/// ## NG
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn hikari_screen_request_redraw(screen: &Screen) -> ApiResult {
    screen.window.request_redraw();
    ApiResult::ok()
}

#[no_mangle]
extern "cdecl" fn hikari_create_command_encoder(
    screen: &Screen,
) -> ApiBoxResult<wgpu::CommandEncoder> {
    let encoder = screen
        .device
        .create_command_encoder(&wgpu::CommandEncoderDescriptor { label: None });
    ApiBoxResult::ok(Box::new(encoder))
}

#[no_mangle]
extern "cdecl" fn hikari_finish_command_encoder(
    screen: &Screen,
    encoder: Box<wgpu::CommandEncoder>,
) {
    screen.queue.submit(std::iter::once(encoder.finish()));
}

#[no_mangle]
extern "cdecl" fn hikari_get_surface_texture(
    screen: &Screen,
) -> ApiValueResult<Option<Box<wgpu::SurfaceTexture>>> {
    match screen.surface.get_current_texture() {
        Ok(surface_texture) => {
            let value = Some(Box::new(surface_texture));
            ApiValueResult::ok(value)
        }
        Err(wgpu::SurfaceError::Lost) => {
            let size = screen.window.inner_size();
            screen.resize_surface(size.width, size.height);
            ApiValueResult::ok(None)
        }
        Err(err) => {
            set_tls_last_error(err);
            ApiValueResult::err()
        }
    }
}

static_assertions::assert_impl_all!(Box<wgpu::SurfaceTexture>: Send, Sync);
static_assertions::assert_impl_all!(wgpu::SurfaceTexture: Send, Sync);

#[no_mangle]
extern "cdecl" fn hikari_destroy_surface_texture(surface_texture: Box<wgpu::SurfaceTexture>) {
    drop(surface_texture);
}

#[no_mangle]
extern "cdecl" fn hikari_surface_texture_to_texture(
    surface_texture: &wgpu::SurfaceTexture,
) -> &wgpu::Texture {
    &surface_texture.texture
}

#[no_mangle]
extern "cdecl" fn hikari_present_surface_texture(surface_texture: Box<wgpu::SurfaceTexture>) {
    surface_texture.present()
}

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn hikari_screen_set_title(screen: &Screen, title: Slice<u8>) -> ApiResult {
    let result = title.as_str().map(|title| {
        screen.window.set_title(title);
    });
    ApiResult::ok_or_set_error(result)
}

/// # Thread Safety
/// Only from main thread.
/// (I do not know if it is thread safe or not. But that's good enough for me.)
/// ## NG
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn hikari_create_render_pass<'tex, 'desc, 'cmd_enc>(
    command_encoder: &'cmd_enc mut wgpu::CommandEncoder,
    desc: &'desc RenderPassDescriptor<'tex, 'desc>,
) -> ApiBoxResult<wgpu::RenderPass<'cmd_enc>>
where
    'tex: 'cmd_enc,
{
    let render_pass = desc.begin_render_pass_with(command_encoder);
    // `command_encoder` is no longer accessible until `render_pass` will drop.
    ApiBoxResult::ok(Box::new(render_pass))
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
extern "cdecl" fn hikari_destroy_render_pass<'cmd_enc>(
    render_pass: Box<wgpu::RenderPass<'cmd_enc>>,
) {
    drop(render_pass)
}

/// # Thread Safety
///
/// `&mut wgpu::CommandEncoder` can not move to another thread because it's mutable reference.
/// This function can be called from any thread, but be careful about the thread of argument `command_encoder`
///
/// # Thread Safety
/// ## OK
/// - called from any thread (Be careful about the thread of argument `command_encoder`)
/// ## NG
/// - called from multiple threads simultaneously with same args with same args
#[no_mangle]
extern "cdecl" fn hikari_create_compute_pass(
    command_encoder: &mut wgpu::CommandEncoder,
) -> ApiBoxResult<wgpu::ComputePass> {
    let compute_pass = command_encoder.begin_compute_pass(&wgpu::ComputePassDescriptor {
        label: None,
        timestamp_writes: None,
    });
    ApiBoxResult::ok(Box::new(compute_pass))
}

/// # Thread Safety
/// ## OK
/// - called from any thread
/// ## NG
/// - called from multiple threads simultaneously with same args with same args
#[no_mangle]
extern "cdecl" fn hikari_destroy_compute_pass(compute_pass: Box<wgpu::ComputePass>) {
    drop(compute_pass)
}

static_assertions::assert_impl_all!(Box<wgpu::ComputePass>: Send, Sync);
static_assertions::assert_impl_all!(wgpu::ComputePass: Send, Sync);

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn hikari_screen_set_inner_size(
    screen: &Screen,
    width: u32,
    height: u32,
) -> ApiResult {
    if let (Some(w), Some(h)) = (NonZeroU32::new(width), NonZeroU32::new(height)) {
        screen.set_inner_size(w, h);
    }
    ApiResult::ok()
}

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn hikari_screen_get_inner_size(screen: &Screen) -> ApiValueResult<SizeU32> {
    let size: (u32, u32) = screen.window.inner_size().into();
    ApiValueResult::ok(size.into())
}

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn hikari_screen_get_scale_factor(screen: &Screen) -> ApiValueResult<f64> {
    let scale_factor = screen.window.scale_factor();
    ApiValueResult::ok(scale_factor)
}

#[no_mangle]
extern "cdecl" fn hikari_screen_set_location(
    screen: &Screen,
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
    ApiResult::ok_or_set_error(f())
}

/// # Thread Safety
/// (iOS) Only from main thread.
/// (`winit::monitor::MonitorHandle` can only be used on the main thread in iOS.)
/// ## NG
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn hikari_screen_get_location(
    screen: &Screen,
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
        Ok((p.x - offset.x, p.y - offset.y).into())
    };
    ApiValueResult::ok_or_set_error(f())
}

/// # Thread Safety
/// (iOS) Only from main thread.
/// (`Window::current_monitor` can only be called on the main thread in iOS.)
/// ## NG
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn hikari_current_monitor(screen: &Screen) -> ApiValueResult<Opt<MonitorId>> {
    let result = screen
        .window
        .current_monitor()
        .map(|monitor| Some(MonitorId::new(monitor)).into())
        .ok_or("no monitors");
    ApiValueResult::ok_or_set_error(result)
}

/// # Thread Safety
/// (iOS) Only from main thread.
/// (`Window::available_monitors` can only be called on the main thread in iOS.)
/// ## NG
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn hikari_monitor_count(screen: &Screen) -> ApiValueResult<usize> {
    let count = screen.window.available_monitors().count();
    ApiValueResult::ok(count)
}

/// # Thread Safety
/// (iOS) Only from main thread.
/// (`Window::available_monitors` can only be called on the main thread in iOS.)
/// ## NG
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn hikari_monitors(
    screen: &Screen,
    buf: *mut MonitorId,
    buflen: usize,
) -> ApiValueResult<usize> {
    let mut i: usize = 0;
    let buf = unsafe { std::slice::from_raw_parts_mut(buf, buflen) };
    for monitor in screen.window.available_monitors().take(buflen) {
        buf[i] = MonitorId::new(monitor);
        i += 1;
    }
    ApiValueResult::ok(i)
}

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn hikari_write_texture(
    screen: &Screen,
    texture: &ImageCopyTexture,
    data: Slice<u8>,
    data_layout: &ImageDataLayout,
    size: &wgpu::Extent3d,
) -> ApiResult {
    screen.queue.write_texture(
        texture.to_wgpu_type(),
        &data,
        data_layout.to_wgpu_type(),
        *size,
    );
    ApiResult::ok()
}

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn hikari_create_bind_group_layout(
    screen: &Screen,
    desc: &BindGroupLayoutDescriptor,
) -> ApiBoxResult<wgpu::BindGroupLayout> {
    let value = desc.use_wgpu_type(|desc| {
        let layout = screen.device.create_bind_group_layout(desc);
        Box::new(layout)
    });
    ApiBoxResult::ok(value)
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
extern "cdecl" fn hikari_destroy_bind_group_layout(layout: Box<wgpu::BindGroupLayout>) {
    drop(layout)
}

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn hikari_create_bind_group(
    screen: &Screen,
    desc: &BindGroupDescriptor,
) -> ApiBoxResult<wgpu::BindGroup> {
    let value = desc.use_wgpu_type(|desc| {
        let bind_group = screen.device.create_bind_group(desc);
        Box::new(bind_group)
    });
    ApiBoxResult::ok(value)
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
extern "cdecl" fn hikari_destroy_bind_group(bind_group: Box<wgpu::BindGroup>) {
    drop(bind_group)
}

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn hikari_create_pipeline_layout(
    screen: &Screen,
    desc: &PipelineLayoutDescriptor,
) -> ApiBoxResult<wgpu::PipelineLayout> {
    let value = screen.device.create_pipeline_layout(&desc.to_wgpu_type());
    let value = Box::new(value);
    ApiBoxResult::ok(value)
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
extern "cdecl" fn hikari_destroy_pipeline_layout(layout: Box<wgpu::PipelineLayout>) {
    drop(layout)
}

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn hikari_create_render_pipeline(
    screen: &Screen,
    desc: &RenderPipelineDescriptor,
) -> ApiBoxResult<wgpu::RenderPipeline> {
    let result = desc.use_wgpu_type(|desc| {
        let value = screen.device.create_render_pipeline(desc);
        Ok(Box::new(value))
    });
    ApiBoxResult::ok_or_set_error(result)
}

static_assertions::assert_impl_all!(RenderPipelineDescriptor: Send, Sync);
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
extern "cdecl" fn hikari_destroy_render_pipeline(pipeline: Box<wgpu::RenderPipeline>) {
    drop(pipeline)
}

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn hikari_create_compute_pipeline(
    screen: &Screen,
    desc: &ComputePipelineDescriptor,
) -> ApiBoxResult<wgpu::ComputePipeline> {
    let result = desc.use_wgpu_type(|desc| {
        let value = screen.device.create_compute_pipeline(desc);
        Ok(Box::new(value))
    });
    ApiBoxResult::ok_or_set_error(result)
}

static_assertions::assert_impl_all!(ComputePipelineDescriptor: Send, Sync);
static_assertions::assert_impl_all!(Box<wgpu::ComputePipeline>: Send, Sync);
static_assertions::assert_impl_all!(wgpu::ComputePipeline: Send, Sync);

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn hikari_destroy_compute_pipeline(pipeline: Box<wgpu::ComputePipeline>) {
    drop(pipeline)
}

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn hikari_create_buffer(
    screen: &Screen,
    size: u64,
    usage: wgpu::BufferUsages,
) -> ApiBoxResult<wgpu::Buffer> {
    let buffer = screen.device.create_buffer(&wgpu::BufferDescriptor {
        label: None,
        size,
        usage,
        mapped_at_creation: false,
    });
    let value = Box::new(buffer);
    ApiBoxResult::ok(value)
}

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn hikari_create_buffer_init(
    screen: &Screen,
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
    ApiBoxResult::ok(value)
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
extern "cdecl" fn hikari_destroy_buffer(buffer: Box<wgpu::Buffer>) {
    drop(buffer)
}

#[no_mangle]
extern "cdecl" fn hikari_copy_texture_to_buffer(
    screen: &Screen,
    source: &ImageCopyTexture,
    copy_size: &wgpu::Extent3d,
    buffer: &wgpu::Buffer,
    image_layout: &ImageDataLayout,
) -> ApiResult {
    if !source
        .texture
        .usage()
        .contains(wgpu::TextureUsages::COPY_SRC)
    {
        engine::set_tls_last_error("texture does not have 'COPY_SRC' flag");
        return ApiResult::err();
    }
    if !buffer.usage().contains(wgpu::BufferUsages::COPY_DST) {
        engine::set_tls_last_error("buffer does not have 'COPY_DST' flag");
        return ApiResult::err();
    }
    let copy_byte_len =
        copy_size.width as u64 * copy_size.height as u64 * copy_size.depth_or_array_layers as u64;
    if buffer.size() < copy_byte_len {
        engine::set_tls_last_error("dest buffer size is too small");
        return ApiResult::err();
    }
    if buffer.size() % wgpu::COPY_BYTES_PER_ROW_ALIGNMENT as u64 != 0 {
        engine::set_tls_last_error(format!(
            "dest buffer size is not aligned to {}",
            wgpu::COPY_BYTES_PER_ROW_ALIGNMENT
        ));
        return ApiResult::err();
    }

    let mut encoder = screen
        .device
        .create_command_encoder(&wgpu::CommandEncoderDescriptor { label: None });
    encoder.copy_texture_to_buffer(
        source.to_wgpu_type(),
        wgpu::TexelCopyBufferInfo {
            buffer,
            layout: image_layout.to_wgpu_type(),
        },
        *copy_size,
    );
    screen.queue.submit(Some(encoder.finish()));
    ApiResult::ok()
}

#[no_mangle]
extern "cdecl" fn hikari_read_buffer(
    screen: &Screen,
    buffer_slice: BufferSlice,
    token: usize,
    callback: extern "cdecl" fn(token: usize, result: ApiResult, view: *const u8, len: usize),
) -> ApiResult {
    wgpu::util::DownloadBuffer::read_buffer(
        &screen.device,
        &screen.queue,
        &buffer_slice.to_wgpu_type(),
        move |result| match result {
            Ok(downloaded) => {
                let view: &[u8] = &downloaded;
                callback(token, ApiResult::ok(), view.as_ptr(), view.len());
            }
            Err(err) => {
                set_tls_last_error(err);
                callback(token, ApiResult::err(), std::ptr::null(), 0);
            }
        },
    );
    ApiResult::ok()
}

static_assertions::assert_impl_all!(SamplerDescriptor: Send, Sync);

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn hikari_create_sampler(
    screen: &Screen,
    desc: &SamplerDescriptor,
) -> ApiBoxResult<wgpu::Sampler> {
    let sampler = screen.device.create_sampler(&desc.to_wgpu_type());
    let value = Box::new(sampler);
    ApiBoxResult::ok(value)
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
extern "cdecl" fn hikari_destroy_sampler(sampler: Box<wgpu::Sampler>) {
    drop(sampler)
}

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn hikari_create_shader_module(
    screen: &Screen,
    shader_source: Slice<u8>,
) -> ApiBoxResult<wgpu::ShaderModule> {
    let result = shader_source.as_str().map(|s| {
        let shader = screen
            .device
            .create_shader_module(wgpu::ShaderModuleDescriptor {
                label: None,
                source: wgpu::ShaderSource::Wgsl(s.into()),
            });
        Box::new(shader)
    });
    ApiBoxResult::ok_or_set_error(result)
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
extern "cdecl" fn hikari_destroy_shader_module(shader: Box<wgpu::ShaderModule>) {
    drop(shader)
}

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn hikari_create_texture(
    screen: &Screen,
    desc: &TextureDescriptor,
) -> ApiBoxResult<wgpu::Texture> {
    let value = Box::new(screen.device.create_texture(&desc.to_wgpu_type()));
    ApiBoxResult::ok(value)
}

#[no_mangle]
extern "cdecl" fn hikari_get_texture_descriptor(
    texture: &wgpu::Texture,
    desc: &mut TextureDescriptor,
) -> ApiResult {
    match texture.format().try_into() {
        Ok(format) => {
            *desc = TextureDescriptor {
                size: texture.size(),
                mip_level_count: texture.mip_level_count(),
                sample_count: texture.sample_count(),
                dimension: texture.dimension().into(),
                format,
                usage: texture.usage(),
            };
            ApiResult::ok()
        }
        Err(err) => {
            set_tls_last_error(err);
            ApiResult::err()
        }
    }
}

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn hikari_create_texture_with_data(
    screen: &Screen,
    desc: &TextureDescriptor,
    data: Slice<u8>,
) -> ApiBoxResult<wgpu::Texture> {
    use wgpu::util::DeviceExt;

    let texture = screen.device.create_texture_with_data(
        &screen.queue,
        &desc.to_wgpu_type(),
        Default::default(),
        &data,
    );
    let value = Box::new(texture);
    ApiBoxResult::ok(value)
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
extern "cdecl" fn hikari_destroy_texture(texture: Box<wgpu::Texture>) {
    drop(texture)
}

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn hikari_texture_format_required_features(
    format: TextureFormat,
) -> ApiValueResult<wgpu::Features> {
    let features: wgpu::Features = format.to_wgpu_type().required_features();
    ApiValueResult::ok(features)
}

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn hikari_texture_format_sample_type(
    format: TextureFormat,
    aspect: Opt<TextureAspect>,
) -> ApiValueResult<Opt<TextureSampleType>> {
    let sample_type: Opt<TextureSampleType> = format
        .to_wgpu_type()
        .sample_type(aspect.map_to_option(|a| a.to_wgpu_type()), None)
        .map(|x| x.into())
        .into();
    ApiValueResult::ok(sample_type)
}

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn hikari_texture_format_block_dimensions(
    format: TextureFormat,
) -> ApiValueResult<Tuple<u32, u32>> {
    let block_dimensions: Tuple<u32, u32> = format.to_wgpu_type().block_dimensions().into();
    ApiValueResult::ok(block_dimensions)
}

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn hikari_texture_format_block_size(
    format: TextureFormat,
    aspect: Opt<TextureAspect>,
) -> ApiValueResult<Opt<u32>> {
    let block_size: Opt<u32> = format
        .to_wgpu_type()
        .block_copy_size(aspect.map_to_option(|a| a.to_wgpu_type()))
        .into();
    ApiValueResult::ok(block_size)
}

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn hikari_texture_format_components(
    format: TextureFormat,
    aspect: TextureAspect,
) -> ApiValueResult<u8> {
    let components: u8 = format
        .to_wgpu_type()
        .components_with_aspect(aspect.to_wgpu_type());
    ApiValueResult::ok(components)
}

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn hikari_texture_format_is_srgb(format: TextureFormat) -> ApiValueResult<bool> {
    let is_srgb = format.to_wgpu_type().is_srgb();
    ApiValueResult::ok(is_srgb)
}

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn hikari_texture_format_guaranteed_format_features(
    screen: &Screen,
    format: TextureFormat,
) -> ApiValueResult<TextureFormatFeatures> {
    let features: TextureFormatFeatures = format
        .to_wgpu_type()
        .guaranteed_format_features(screen.device.features())
        .into();
    ApiValueResult::ok(features)
}

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn hikari_create_texture_view(
    texture: &wgpu::Texture,
    desc: &TextureViewDescriptor,
) -> ApiBoxResult<wgpu::TextureView> {
    let desc = &desc.to_wgpu_type();
    let value = Box::new(texture.create_view(desc));
    ApiBoxResult::ok(value)
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
extern "cdecl" fn hikari_destroy_texture_view(texture_view: Box<wgpu::TextureView>) {
    drop(texture_view)
}

/// # Thread Safety
/// ## OK
/// - called from any thread
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn hikari_write_buffer(
    screen: &Screen,
    buffer: &wgpu::Buffer,
    offset: u64,
    data: Slice<u8>,
) -> ApiResult {
    screen.queue.write_buffer(buffer, offset, &data);
    ApiResult::ok()
}

static_assertions::assert_impl_all!(wgpu::RenderPass: Send, Sync);
static_assertions::assert_impl_all!(wgpu::RenderPipeline: Send, Sync);

#[no_mangle]
extern "cdecl" fn hikari_compute_set_pipeline<'a>(
    pass: &'a mut wgpu::ComputePass<'a>,
    pipeline: &'a wgpu::ComputePipeline,
) -> ApiResult {
    pass.set_pipeline(pipeline);
    ApiResult::ok()
}

#[no_mangle]
extern "cdecl" fn hikari_compute_set_bind_group<'a>(
    pass: &'a mut wgpu::ComputePass<'a>,
    index: u32,
    bind_group: &'a wgpu::BindGroup,
) -> ApiResult {
    pass.set_bind_group(index, bind_group, &[]);
    ApiResult::ok()
}

#[no_mangle]
extern "cdecl" fn hikari_compute_dispatch_workgroups<'a>(
    pass: &'a mut wgpu::ComputePass<'a>,
    x: u32,
    y: u32,
    z: u32,
) -> ApiResult {
    pass.dispatch_workgroups(x, y, z);
    ApiResult::ok()
}

/// # Thread Safety
/// It cannot be called at the same time as other functions that use same `&mut wgpu::RenderPass`.
/// Multiple mutable references cannot exist simultaneously.
/// ## OK
/// - called from any thread
/// ## NG
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn hikari_set_pipeline<'a>(
    render_pass: &mut wgpu::RenderPass<'a>,
    render_pipeline: &'a wgpu::RenderPipeline,
) -> ApiResult {
    render_pass.set_pipeline(render_pipeline);
    ApiResult::ok()
}

/// # Thread Safety
/// It cannot be called at the same time as other functions that use same `&mut wgpu::RenderPass`.
/// Multiple mutable references cannot exist simultaneously.
/// ## OK
/// - called from any thread
/// ## NG
/// - called from multiple threads simultaneously with same args
#[no_mangle]
extern "cdecl" fn hikari_set_viewport<'a>(
    render_pass: &mut wgpu::RenderPass<'a>,
    x: f32,
    y: f32,
    w: f32,
    h: f32,
    min_depth: f32,
    max_depth: f32,
) -> ApiResult {
    render_pass.set_viewport(x, y, w, h, min_depth, max_depth);
    ApiResult::ok()
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
extern "cdecl" fn hikari_set_bind_group<'a>(
    render_pass: &mut wgpu::RenderPass<'a>,
    index: u32,
    bind_group: &'a wgpu::BindGroup,
) -> ApiResult {
    render_pass.set_bind_group(index, bind_group, &[]);
    ApiResult::ok()
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
extern "cdecl" fn hikari_set_vertex_buffer<'a>(
    render_pass: &mut wgpu::RenderPass<'a>,
    slot: u32,
    buffer_slice: BufferSlice<'a>,
) -> ApiResult {
    render_pass.set_vertex_buffer(slot, buffer_slice.to_wgpu_type());
    ApiResult::ok()
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
extern "cdecl" fn hikari_set_index_buffer<'a>(
    render_pass: &mut wgpu::RenderPass<'a>,
    buffer_slice: BufferSlice<'a>,
    index_format: wgpu::IndexFormat,
) -> ApiResult {
    render_pass.set_index_buffer(buffer_slice.to_wgpu_type(), index_format);
    ApiResult::ok()
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
extern "cdecl" fn hikari_draw<'a>(
    render_pass: &mut wgpu::RenderPass<'a>,
    vertices: RangeU32,
    instances: RangeU32,
) -> ApiResult {
    render_pass.draw(vertices.to_range(), instances.to_range());
    ApiResult::ok()
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
extern "cdecl" fn hikari_draw_indexed<'a>(
    render_pass: &mut wgpu::RenderPass<'a>,
    indices: RangeU32,
    base_vertex: i32,
    instances: RangeU32,
) -> ApiResult {
    render_pass.draw_indexed(indices.to_range(), base_vertex, instances.to_range());
    ApiResult::ok()
}

static_assertions::assert_impl_all!(winit::window::Window: Send, Sync);

#[no_mangle]
extern "cdecl" fn hikari_set_ime_allowed(screen: &Screen, allowed: bool) -> ApiResult {
    screen.window.set_ime_allowed(allowed);
    ApiResult::ok()
}

#[no_mangle]
extern "cdecl" fn hikari_set_ime_position(screen: &Screen, x: u32, y: u32) -> ApiResult {
    let pos = winit::dpi::PhysicalPosition::new(x, y);
    screen
        .window
        .set_ime_cursor_area(pos, PhysicalSize::new(30, 30));
    ApiResult::ok()
}

#[no_mangle]
extern "cdecl" fn hikari_get_tls_last_error_len() -> usize {
    engine::get_tls_last_error_len()
}

#[no_mangle]
extern "cdecl" fn hikari_take_tls_last_error(buf: *mut u8) {
    let error = engine::take_tls_last_error();
    let bytes = error.as_bytes();
    let slice = unsafe { std::slice::from_raw_parts_mut(buf, bytes.len()) };
    slice.copy_from_slice(bytes);
}
