mod engine;
mod ffi;
mod screen;

use crate::screen::{Screen, ScreenId};
use corehikari_macros::tagged_ref_union;
use smallvec::SmallVec;
use static_assertions::assert_eq_size;
use std;
use std::error::Error;
use std::{mem, num, ops, str};
use winit::event::Ime;
use winit::window;

#[repr(C)]
#[derive(Debug, Clone, Copy)]
pub(crate) struct EngineCoreConfig {
    pub on_screen_init: ScreenInitFn,
    pub on_unhandled_error: EngineUnhandledErrorFn,
    pub event_cleared: ClearedEventFn,
    pub event_redraw_requested: RedrawRequestedEventFn,
    pub event_resized: ResizedEventFn,
    pub event_keyboard: KeyboardEventFn,
    pub event_char_received: CharReceivedEventFn,
    pub event_mouse_button: MouseButtonEventFn,
    pub event_ime: ImeInputEventFn,
    pub event_wheel: MouseWheelEventFn,
    pub event_cursor_moved: CursorMovedEventFn,
    pub event_cursor_entered_left: CursorEnteredLeftEventFn,
    pub event_closing: ClosingEventFn,
    pub event_closed: ClosedEventFn,
    pub debug_println: DebugPrintlnFn,
}

#[repr(C)]
#[derive(Debug, Clone, Copy)]
pub(crate) struct ScreenConfig {
    pub style: WindowStyle,
    pub width: u32,
    pub height: u32,
    pub backend: wgpu::Backends,
    pub present_mode: PresentMode,
}

#[repr(u32)]
#[derive(Debug, PartialEq, Eq, Clone, Copy)]
#[allow(dead_code)] // because values are from FFI
pub(crate) enum PresentMode {
    AutoVsync = 0,
    AutoNoVsync = 1,
    Fifo = 2,
    FifoRelaxed = 3,
    Immediate = 4,
    Mailbox = 5,
}

impl PresentMode {
    pub fn to_wgpu_type(&self) -> wgpu::PresentMode {
        match self {
            Self::AutoVsync => wgpu::PresentMode::AutoVsync,
            Self::AutoNoVsync => wgpu::PresentMode::AutoNoVsync,
            Self::Fifo => wgpu::PresentMode::Fifo,
            Self::FifoRelaxed => wgpu::PresentMode::FifoRelaxed,
            Self::Immediate => wgpu::PresentMode::Immediate,
            Self::Mailbox => wgpu::PresentMode::Mailbox,
        }
    }
}

#[repr(u32)]
#[derive(Debug, PartialEq, Eq, Clone, Copy)]
#[allow(dead_code)] // because values are from FFI
pub(crate) enum WindowStyle {
    Default = 0,
    Fixed = 1,
    Fullscreen = 2,
}

#[repr(C)]
pub(crate) struct RenderPassDescriptor<'tex, 'desc> {
    pub color_attachments: Slice<'desc, Opt<RenderPassColorAttachment<'tex>>>,
    pub depth_stencil_attachment: Opt<RenderPassDepthStencilAttachment<'tex>>,
}

impl<'tex, 'desc> RenderPassDescriptor<'tex, 'desc> {
    pub fn begin_render_pass_with<'enc>(
        &self,
        command_encoder: &'enc mut wgpu::CommandEncoder,
    ) -> wgpu::RenderPass<'enc>
    where
        'tex: 'enc,
    {
        let color_attachments: Vec<_> = self
            .color_attachments
            .iter()
            .map(|opt| opt.map_to_option(|value| value.to_wgpu_type()))
            .collect();
        let desc = wgpu::RenderPassDescriptor {
            label: None,
            timestamp_writes: None,
            occlusion_query_set: None,
            color_attachments: &color_attachments,
            depth_stencil_attachment: self
                .depth_stencil_attachment
                .map_to_option(|x| x.to_wgpu_type()),
        };
        command_encoder.begin_render_pass(&desc)
    }
}

#[repr(C)]
#[derive(Debug)]
pub(crate) struct RenderPassColorAttachment<'tex> {
    pub view: &'tex wgpu::TextureView,
    pub init: RenderPassColorBufferInit,
}

impl<'tex> RenderPassColorAttachment<'tex> {
    pub fn to_wgpu_type(&self) -> wgpu::RenderPassColorAttachment<'tex> {
        wgpu::RenderPassColorAttachment {
            view: self.view,
            resolve_target: None,
            ops: wgpu::Operations {
                load: self.init.to_wgpu_type(),
                store: wgpu::StoreOp::Store,
            },
        }
    }
}

#[repr(C)]
pub(crate) struct RenderPassDepthStencilAttachment<'tex> {
    pub view: &'tex wgpu::TextureView,
    pub depth: Opt<RenderPassDepthBufferInit>,
    pub stencil: Opt<RenderPassStencilBufferInit>,
}

impl<'tex> RenderPassDepthStencilAttachment<'tex> {
    pub fn to_wgpu_type(&self) -> wgpu::RenderPassDepthStencilAttachment<'tex> {
        wgpu::RenderPassDepthStencilAttachment {
            view: self.view,
            depth_ops: self.depth.map_to_option(|x| wgpu::Operations {
                load: x.to_wgpu_type(),
                store: wgpu::StoreOp::Store,
            }),
            stencil_ops: self.stencil.map_to_option(|x| wgpu::Operations {
                load: x.to_wgpu_type(),
                store: wgpu::StoreOp::Store,
            }),
        }
    }
}

#[repr(u32)]
#[derive(Debug)]
#[allow(dead_code)] // because values are from FFI
pub(crate) enum RenderPassBufferInitMode {
    Clear = 0,
    Load = 1,
}

#[repr(C)]
#[derive(Debug)]
pub(crate) struct RenderPassColorBufferInit {
    pub mode: RenderPassBufferInitMode,
    pub value: wgpu::Color,
}

impl RenderPassColorBufferInit {
    pub fn to_wgpu_type(&self) -> wgpu::LoadOp<wgpu::Color> {
        match self.mode {
            RenderPassBufferInitMode::Clear => wgpu::LoadOp::Clear(self.value),
            RenderPassBufferInitMode::Load => wgpu::LoadOp::Load,
        }
    }
}

#[repr(C)]
#[derive(Debug)]
pub(crate) struct RenderPassDepthBufferInit {
    pub mode: RenderPassBufferInitMode,
    pub value: f32,
}

impl RenderPassDepthBufferInit {
    pub fn to_wgpu_type(&self) -> wgpu::LoadOp<f32> {
        match self.mode {
            RenderPassBufferInitMode::Clear => wgpu::LoadOp::Clear(self.value),
            RenderPassBufferInitMode::Load => wgpu::LoadOp::Load,
        }
    }
}

#[repr(C)]
#[derive(Debug)]
pub(crate) struct RenderPassStencilBufferInit {
    pub mode: RenderPassBufferInitMode,
    pub value: u32,
}

impl RenderPassStencilBufferInit {
    pub fn to_wgpu_type(&self) -> wgpu::LoadOp<u32> {
        match self.mode {
            RenderPassBufferInitMode::Clear => wgpu::LoadOp::Clear(self.value),
            RenderPassBufferInitMode::Load => wgpu::LoadOp::Load,
        }
    }
}

#[repr(C)]
pub(crate) struct BindGroupLayoutDescriptor<'a> {
    pub entries: Slice<'a, BindGroupLayoutEntry<'a>>,
}

impl<'a> BindGroupLayoutDescriptor<'a> {
    pub fn use_wgpu_type<T>(
        &self,
        consume: impl FnOnce(&wgpu::BindGroupLayoutDescriptor) -> T,
    ) -> T {
        let desc = wgpu::BindGroupLayoutDescriptor {
            label: None,
            entries: &self
                .entries
                .iter()
                .map(|x| x.to_wgpu_type())
                .collect::<SmallVec<[_; 16]>>(),
        };
        consume(&desc)
    }
}

#[repr(C)]
pub(crate) struct ImageCopyTexture<'a> {
    pub texture: &'a wgpu::Texture,
    pub mip_level: u32,
    pub origin_x: u32,
    pub origin_y: u32,
    pub origin_z: u32,
    pub aspect: TextureAspect,
}

impl<'a> ImageCopyTexture<'a> {
    pub const fn to_wgpu_type(&self) -> wgpu::TexelCopyTextureInfo {
        wgpu::TexelCopyTextureInfo {
            texture: self.texture,
            mip_level: self.mip_level,
            origin: wgpu::Origin3d {
                x: self.origin_x,
                y: self.origin_y,
                z: self.origin_z,
            },
            aspect: self.aspect.to_wgpu_type(),
        }
    }
}

#[repr(C)]
#[derive(Clone, Copy, Debug, Default)]
pub(crate) struct ImageDataLayout {
    pub offset: u64,
    pub bytes_per_row: u32,
    pub rows_per_image: u32,
}

impl ImageDataLayout {
    pub fn to_wgpu_type(&self) -> wgpu::TexelCopyBufferLayout {
        wgpu::TexelCopyBufferLayout {
            offset: self.offset,
            bytes_per_row: Some(self.bytes_per_row),
            rows_per_image: Some(self.rows_per_image),
        }
    }
}

#[repr(C)]
pub(crate) struct TextureViewDescriptor {
    pub format: Opt<TextureFormat>,
    pub dimension: Opt<TextureViewDimension>,
    pub aspect: TextureAspect,
    pub base_mip_level: u32,
    pub mip_level_count: Opt<u32>,
    pub base_array_layer: u32,
    pub array_layer_count: Opt<u32>,
}

impl TextureViewDescriptor {
    pub fn to_wgpu_type(&self) -> wgpu::TextureViewDescriptor {
        wgpu::TextureViewDescriptor {
            label: None,
            usage: None,
            format: self.format.map_to_option(|x| x.to_wgpu_type()),
            dimension: self.dimension.map_to_option(|x| x.to_wgpu_type()),
            aspect: self.aspect.to_wgpu_type(),
            base_mip_level: self.base_mip_level,
            mip_level_count: self.mip_level_count.to_option(),
            base_array_layer: self.base_array_layer,
            array_layer_count: self.array_layer_count.to_option(),
        }
    }
}

#[repr(u32)]
#[derive(Debug, PartialEq, Eq, Clone, Copy)]
#[allow(dead_code)] // because values are from FFI
pub(crate) enum TextureAspect {
    All = 0,
    StencilOnly = 1,
    DepthOnly = 2,
}

impl TextureAspect {
    pub const fn to_wgpu_type(&self) -> wgpu::TextureAspect {
        match self {
            Self::All => wgpu::TextureAspect::All,
            Self::StencilOnly => wgpu::TextureAspect::StencilOnly,
            Self::DepthOnly => wgpu::TextureAspect::DepthOnly,
        }
    }
}

#[repr(C)]
pub(crate) struct SamplerDescriptor {
    pub address_mode_u: wgpu::AddressMode,
    pub address_mode_v: wgpu::AddressMode,
    pub address_mode_w: wgpu::AddressMode,
    pub mag_filter: wgpu::FilterMode,
    pub min_filter: wgpu::FilterMode,
    pub mipmap_filter: wgpu::FilterMode,
    pub lod_min_clamp: f32,
    pub lod_max_clamp: f32,
    pub compare: Opt<wgpu::CompareFunction>,
    pub anisotropy_clamp: u16,
    pub border_color: Opt<SamplerBorderColor>,
}

impl SamplerDescriptor {
    pub fn to_wgpu_type(&self) -> wgpu::SamplerDescriptor {
        wgpu::SamplerDescriptor {
            label: None,
            address_mode_u: self.address_mode_u,
            address_mode_v: self.address_mode_v,
            address_mode_w: self.address_mode_w,
            mag_filter: self.mag_filter,
            min_filter: self.min_filter,
            mipmap_filter: self.mipmap_filter,
            lod_min_clamp: self.lod_min_clamp,
            lod_max_clamp: self.lod_max_clamp,
            compare: self.compare.to_option(),
            anisotropy_clamp: self.anisotropy_clamp,
            border_color: self.border_color.map_to_option(|x| x.to_wgpu_type()),
        }
    }
}

#[repr(u32)]
#[derive(Debug, PartialEq, Eq, Clone, Copy)]
#[allow(dead_code)] // because values are from FFI
pub(crate) enum SamplerBorderColor {
    TransparentBlack = 0,
    OpaqueBlack = 1,
    OpaqueWhite = 2,
    Zero = 3,
}

impl SamplerBorderColor {
    pub const fn to_wgpu_type(&self) -> wgpu::SamplerBorderColor {
        match self {
            Self::TransparentBlack => wgpu::SamplerBorderColor::TransparentBlack,
            Self::OpaqueBlack => wgpu::SamplerBorderColor::OpaqueBlack,
            Self::OpaqueWhite => wgpu::SamplerBorderColor::OpaqueWhite,
            Self::Zero => wgpu::SamplerBorderColor::Zero,
        }
    }
}

#[repr(C)]
pub(crate) struct BindGroupDescriptor<'a> {
    pub layout: &'a wgpu::BindGroupLayout,
    pub entries: Slice<'a, BindGroupEntry<'a>>,
}

impl<'a> BindGroupDescriptor<'a> {
    pub fn use_wgpu_type<T>(&self, consume: impl FnOnce(&wgpu::BindGroupDescriptor) -> T) -> T {
        use BindingResourceEnum as E;

        let wgpu_bindings_vec = self
            .entries
            .iter()
            .map(|entry| match entry.resource.to_enum() {
                E::BufferArray(bindings) => bindings
                    .iter()
                    .map(|x| x.to_wgpu_type())
                    .collect::<Vec<_>>(),
                _ => vec![],
            })
            .collect::<Vec<_>>();

        let wgpu_group_entries: Vec<_> = self
            .entries
            .iter()
            .enumerate()
            .map(|(i, entry)| {
                let resource = match entry.resource.to_enum() {
                    E::Buffer(binding) => wgpu::BindingResource::Buffer(binding.to_wgpu_type()),
                    E::BufferArray(_) => wgpu::BindingResource::BufferArray(&wgpu_bindings_vec[i]),
                    E::Sampler(sampler) => wgpu::BindingResource::Sampler(sampler),
                    E::SamplerArray(samplers) => wgpu::BindingResource::SamplerArray(samplers),
                    E::TextureView(tex_view) => wgpu::BindingResource::TextureView(tex_view),
                    E::TextureViewArray(tex_views) => {
                        wgpu::BindingResource::TextureViewArray(tex_views)
                    }
                };
                wgpu::BindGroupEntry {
                    binding: entry.binding,
                    resource,
                }
            })
            .collect();
        let desc = wgpu::BindGroupDescriptor {
            label: None,
            layout: self.layout,
            entries: &wgpu_group_entries,
        };
        consume(&desc)
    }
}

#[repr(C)]
pub(crate) struct BindGroupEntry<'a> {
    pub binding: u32,
    pub resource: BindingResource<'a>,
}

#[tagged_ref_union(
    "Buffer@BufferBinding<'a>",
    "BufferArray@Slice<'a, BufferBinding<'a>>",
    "Sampler@wgpu::Sampler",
    "SamplerArray@Slice<'a, &'a wgpu::Sampler>",
    "TextureView@wgpu::TextureView",
    "TextureViewArray@Slice<'a, &'a wgpu::TextureView>"
)]
pub(crate) struct BindingResource;

#[repr(C)]
pub(crate) struct BufferBinding<'a> {
    pub buffer: &'a wgpu::Buffer,
    pub offset: u64,
    pub size: u64,
}

impl<'a> BufferBinding<'a> {
    pub fn to_wgpu_type(&self) -> wgpu::BufferBinding {
        wgpu::BufferBinding {
            buffer: self.buffer,
            offset: self.offset,
            size: num::NonZeroU64::new(self.size),
        }
    }
}

#[repr(C)]
pub(crate) struct PipelineLayoutDescriptor<'a> {
    pub bind_group_layouts: Slice<'a, &'a wgpu::BindGroupLayout>,
}

impl<'a> PipelineLayoutDescriptor<'a> {
    pub fn to_wgpu_type(&self) -> wgpu::PipelineLayoutDescriptor {
        wgpu::PipelineLayoutDescriptor {
            label: None,
            bind_group_layouts: &self.bind_group_layouts,
            push_constant_ranges: &[],
        }
    }
}

#[repr(C)]
pub(crate) struct RenderPipelineDescriptor<'a> {
    pub layout: &'a wgpu::PipelineLayout,
    pub vertex: VertexState<'a>,
    pub fragment: Opt<FragmentState<'a>>,
    pub primitive: PrimitiveState,
    pub depth_stencil: Opt<DepthStencilState>,
    pub multisample: wgpu::MultisampleState,
    pub multiview: Option<num::NonZeroU32>,
}

impl<'a> RenderPipelineDescriptor<'a> {
    pub fn use_wgpu_type<T>(
        &self,
        consume: impl FnOnce(&wgpu::RenderPipelineDescriptor) -> Result<T, Box<dyn Error>>,
    ) -> Result<T, Box<dyn Error>> {
        let vertex = wgpu::VertexState {
            module: self.vertex.module,
            compilation_options: Default::default(),
            entry_point: Some(self.vertex.entry_point.as_str()?),
            buffers: &self
                .vertex
                .buffers
                .iter()
                .map(|x| x.to_wgpu_type())
                .collect::<SmallVec<[_; 4]>>(),
        };

        let fragment_targets: Vec<_>;
        let fragment = match self.fragment.to_ref_option() {
            Some(fragment) => {
                fragment_targets = fragment
                    .targets
                    .iter()
                    .map(|x| x.map_to_option(|y| y.to_wgpu_type()))
                    .collect();
                Some(wgpu::FragmentState {
                    module: fragment.module,
                    compilation_options: Default::default(),
                    entry_point: Some(fragment.entry_point.as_str()?),
                    targets: &fragment_targets,
                })
            }
            None => None,
        };

        let pipeline_desc = wgpu::RenderPipelineDescriptor {
            label: None,
            cache: None,
            layout: Some(self.layout),
            vertex,
            fragment,
            primitive: self.primitive.to_wgpu_type(),
            depth_stencil: self.depth_stencil.map_to_option(|x| x.to_wgpu_type()),
            multisample: self.multisample,
            multiview: self.multiview,
        };
        consume(&pipeline_desc)
    }
}

#[repr(C)]
pub(crate) struct ComputePipelineDescriptor<'a> {
    pub layout: &'a wgpu::PipelineLayout,
    pub module: &'a wgpu::ShaderModule,
    pub entry_point: Slice<'a, u8>,
}

impl<'a> ComputePipelineDescriptor<'a> {
    pub fn use_wgpu_type<T>(
        &self,
        consume: impl FnOnce(&wgpu::ComputePipelineDescriptor) -> Result<T, Box<dyn Error>>,
    ) -> Result<T, Box<dyn Error>> {
        let pipeline_desc = wgpu::ComputePipelineDescriptor {
            label: None,
            compilation_options: Default::default(),
            cache: None,
            layout: Some(self.layout),
            module: self.module,
            entry_point: Some(self.entry_point.as_str()?),
        };
        consume(&pipeline_desc)
    }
}

#[repr(C)]
pub(crate) struct DepthStencilState {
    pub format: TextureFormat,
    pub depth_write_enabled: bool,
    pub depth_compare: wgpu::CompareFunction,
    pub stencil: wgpu::StencilState,
    pub bias: wgpu::DepthBiasState,
}

impl DepthStencilState {
    pub fn to_wgpu_type(&self) -> wgpu::DepthStencilState {
        wgpu::DepthStencilState {
            format: self.format.to_wgpu_type(),
            depth_write_enabled: self.depth_write_enabled,
            depth_compare: self.depth_compare,
            stencil: self.stencil.clone(),
            bias: self.bias,
        }
    }
}

#[repr(C)]
pub(crate) struct VertexState<'a> {
    pub module: &'a wgpu::ShaderModule,
    pub entry_point: Slice<'a, u8>,
    pub buffers: Slice<'a, VertexBufferLayout<'a>>,
}

#[repr(C)]
pub(crate) struct FragmentState<'a> {
    pub module: &'a wgpu::ShaderModule,
    pub entry_point: Slice<'a, u8>,
    pub targets: Slice<'a, Opt<ColorTargetState>>,
}

#[repr(C)]
pub(crate) struct ColorTargetState {
    pub format: TextureFormat,
    pub blend: Opt<wgpu::BlendState>,
    pub write_mask: wgpu::ColorWrites,
}
impl ColorTargetState {
    pub fn to_wgpu_type(&self) -> wgpu::ColorTargetState {
        wgpu::ColorTargetState {
            format: self.format.to_wgpu_type(),
            blend: self.blend.to_option(),
            write_mask: self.write_mask,
        }
    }
}

assert_eq_size!(wgpu::VertexFormat, u32);
assert_eq_size!(wgpu::Face, u32);
assert_eq_size!(wgpu::BlendFactor, u32);
assert_eq_size!(wgpu::BlendOperation, u32);
assert_eq_size!(wgpu::PrimitiveTopology, u32);
assert_eq_size!(wgpu::IndexFormat, u32);
assert_eq_size!(wgpu::FrontFace, u32);
assert_eq_size!(wgpu::PolygonMode, u32);
assert_eq_size!(wgpu::VertexStepMode, u32);

#[repr(C)]
pub(crate) struct PrimitiveState {
    pub topology: wgpu::PrimitiveTopology,
    pub strip_index_format: Opt<wgpu::IndexFormat>,
    pub front_face: wgpu::FrontFace,
    pub cull_mode: Opt<wgpu::Face>,
    pub polygon_mode: wgpu::PolygonMode,
}

impl PrimitiveState {
    pub fn to_wgpu_type(&self) -> wgpu::PrimitiveState {
        wgpu::PrimitiveState {
            topology: self.topology,
            strip_index_format: self.strip_index_format.to_option(),
            front_face: self.front_face,
            cull_mode: self.cull_mode.to_option(),
            unclipped_depth: false,
            polygon_mode: self.polygon_mode,
            conservative: false,
        }
    }
}

#[repr(C)]
pub(crate) struct BindGroupLayoutEntry<'a> {
    pub binding: u32,
    pub visibility: wgpu::ShaderStages,
    pub ty: BindingType<'a>,
    pub count: u32,
}

impl<'a> BindGroupLayoutEntry<'a> {
    pub fn to_wgpu_type(&self) -> wgpu::BindGroupLayoutEntry {
        wgpu::BindGroupLayoutEntry {
            binding: self.binding,
            visibility: self.visibility,
            ty: self.ty.to_wgpu_type(),
            count: num::NonZeroU32::new(self.count),
        }
    }
}

#[tagged_ref_union(
    "Buffer@BufferBindingData",
    "Sampler@SamplerBindingType",
    "Texture@TextureBindingData",
    "StorageTexture@StorageTextureBindingData"
)]
pub(crate) struct BindingType;

impl<'a> BindingType<'a> {
    pub fn to_wgpu_type(&self) -> wgpu::BindingType {
        use BindingTypeEnum as E;
        match self.to_enum() {
            E::Buffer(x) => wgpu::BindingType::Buffer {
                ty: x.ty.to_wgpu_type(),
                has_dynamic_offset: x.has_dynamic_offset,
                min_binding_size: num::NonZeroU64::new(x.min_binding_size),
            },
            E::Sampler(x) => wgpu::BindingType::Sampler(x.to_wgpu_type()),
            E::Texture(x) => wgpu::BindingType::Texture {
                sample_type: x.sample_type.to_wgpu_type(),
                view_dimension: x.view_dimension.to_wgpu_type(),
                multisampled: x.multisampled,
            },
            E::StorageTexture(x) => wgpu::BindingType::StorageTexture {
                access: x.access.to_wgpu_type(),
                format: x.format.to_wgpu_type(),
                view_dimension: x.view_dimension.to_wgpu_type(),
            },
        }
    }
}

#[repr(C)]
pub(crate) struct BufferBindingData {
    pub ty: BufferBindingType,
    pub has_dynamic_offset: bool,
    pub min_binding_size: u64,
}

#[repr(u32)]
#[derive(Debug, PartialEq, Eq, Clone, Copy)]
#[allow(dead_code)] // because values are from FFI
pub(crate) enum BufferBindingType {
    Uniform = 0,
    Storate = 1,
    StorateReadOnly = 2,
}

impl BufferBindingType {
    pub fn to_wgpu_type(&self) -> wgpu::BufferBindingType {
        match self {
            Self::Uniform => wgpu::BufferBindingType::Uniform,
            Self::Storate => wgpu::BufferBindingType::Storage { read_only: false },
            Self::StorateReadOnly => wgpu::BufferBindingType::Storage { read_only: true },
        }
    }
}

#[repr(u32)]
#[derive(Debug, PartialEq, Eq, Clone, Copy)]
#[allow(dead_code)] // because values are from FFI
pub(crate) enum SamplerBindingType {
    Filtering = 0,
    NonFiltering = 1,
    Comparison = 2,
}

impl SamplerBindingType {
    pub fn to_wgpu_type(&self) -> wgpu::SamplerBindingType {
        match self {
            Self::Filtering => wgpu::SamplerBindingType::Filtering,
            Self::NonFiltering => wgpu::SamplerBindingType::NonFiltering,
            Self::Comparison => wgpu::SamplerBindingType::Comparison,
        }
    }
}

#[repr(C)]
pub(crate) struct TextureBindingData {
    pub sample_type: TextureSampleType,
    pub view_dimension: TextureViewDimension,
    pub multisampled: bool,
}

#[repr(u32)]
#[derive(Debug, PartialEq, Eq, Clone, Copy, Default)]
#[allow(dead_code)] // because values are from FFI
pub(crate) enum TextureSampleType {
    #[default]
    FloatFilterable = 0,
    FloatNotFilterable = 1,
    Depth = 2,
    Sint = 3,
    Uint = 4,
}

impl From<wgpu::TextureSampleType> for TextureSampleType {
    fn from(value: wgpu::TextureSampleType) -> Self {
        use wgpu::TextureSampleType as T;
        match value {
            T::Float { filterable } => {
                if filterable {
                    Self::FloatFilterable
                } else {
                    Self::FloatNotFilterable
                }
            }
            T::Depth => Self::Depth,
            T::Sint => Self::Sint,
            T::Uint => Self::Uint,
        }
    }
}

impl TextureSampleType {
    pub fn to_wgpu_type(&self) -> wgpu::TextureSampleType {
        match self {
            Self::FloatFilterable => wgpu::TextureSampleType::Float { filterable: true },
            Self::FloatNotFilterable => wgpu::TextureSampleType::Float { filterable: false },
            Self::Depth => wgpu::TextureSampleType::Depth,
            Self::Sint => wgpu::TextureSampleType::Sint,
            Self::Uint => wgpu::TextureSampleType::Uint,
        }
    }
}

#[repr(u32)]
#[derive(Debug, PartialEq, Eq, Clone, Copy)]
#[allow(dead_code)] // because values are from FFI
pub(crate) enum TextureViewDimension {
    D1 = 0,
    D2 = 1,
    D2Array = 2,
    Cube = 3,
    CubeArray = 4,
    D3 = 5,
}

impl TextureViewDimension {
    pub fn to_wgpu_type(&self) -> wgpu::TextureViewDimension {
        match self {
            Self::D1 => wgpu::TextureViewDimension::D1,
            Self::D2 => wgpu::TextureViewDimension::D2,
            Self::D2Array => wgpu::TextureViewDimension::D2Array,
            Self::Cube => wgpu::TextureViewDimension::Cube,
            Self::CubeArray => wgpu::TextureViewDimension::CubeArray,
            Self::D3 => wgpu::TextureViewDimension::D3,
        }
    }
}

#[repr(C)]
pub(crate) struct StorageTextureBindingData {
    pub access: StorageTextureAccess,
    pub format: TextureFormat,
    pub view_dimension: TextureViewDimension,
}

#[repr(u32)]
#[derive(Debug, PartialEq, Eq, Clone, Copy)]
#[allow(dead_code)] // because values are from FFI
pub(crate) enum StorageTextureAccess {
    WriteOnly = 0,
    ReadOnly = 1,
    ReadWrite = 2,
}

impl StorageTextureAccess {
    pub fn to_wgpu_type(&self) -> wgpu::StorageTextureAccess {
        match self {
            Self::WriteOnly => wgpu::StorageTextureAccess::WriteOnly,
            Self::ReadOnly => wgpu::StorageTextureAccess::ReadOnly,
            Self::ReadWrite => wgpu::StorageTextureAccess::ReadWrite,
        }
    }
}

#[repr(u32)]
#[derive(Debug, PartialEq, Eq, Clone, Copy)]
#[allow(dead_code)] // because values are from FFI
pub(crate) enum TextureFormat {
    // Normal 8 bit formats
    /// Red channel only. 8 bit integer per channel. [0, 255] converted to/from float [0, 1] in shader.
    R8Unorm = 0,
    /// Red channel only. 8 bit integer per channel. [-127, 127] converted to/from float [-1, 1] in shader.
    R8Snorm = 1,
    /// Red channel only. 8 bit integer per channel. Unsigned in shader.
    R8Uint = 2,
    /// Red channel only. 8 bit integer per channel. Signed in shader.
    R8Sint = 3,

    // Normal 16 bit formats
    /// Red channel only. 16 bit integer per channel. Unsigned in shader.
    R16Uint = 4,
    /// Red channel only. 16 bit integer per channel. Signed in shader.
    R16Sint = 5,
    /// Red channel only. 16 bit integer per channel. [0, 65535] converted to/from float [0, 1] in shader.
    ///
    /// [`Features::TEXTURE_FORMAT_16BIT_NORM`] must be enabled to use this texture format.
    R16Unorm = 6,
    /// Red channel only. 16 bit integer per channel. [0, 65535] converted to/from float [-1, 1] in shader.
    ///
    /// [`Features::TEXTURE_FORMAT_16BIT_NORM`] must be enabled to use this texture format.
    R16Snorm = 7,
    /// Red channel only. 16 bit float per channel. Float in shader.
    R16Float = 8,
    /// Red and green channels. 8 bit integer per channel. [0, 255] converted to/from float [0, 1] in shader.
    Rg8Unorm = 9,
    /// Red and green channels. 8 bit integer per channel. [-127, 127] converted to/from float [-1, 1] in shader.
    Rg8Snorm = 10,
    /// Red and green channels. 8 bit integer per channel. Unsigned in shader.
    Rg8Uint = 11,
    /// Red and green channels. 8 bit integer per channel. Signed in shader.
    Rg8Sint = 12,

    // Normal 32 bit formats
    /// Red channel only. 32 bit integer per channel. Unsigned in shader.
    R32Uint = 13,
    /// Red channel only. 32 bit integer per channel. Signed in shader.
    R32Sint = 14,
    /// Red channel only. 32 bit float per channel. Float in shader.
    R32Float = 15,
    /// Red and green channels. 16 bit integer per channel. Unsigned in shader.
    Rg16Uint = 16,
    /// Red and green channels. 16 bit integer per channel. Signed in shader.
    Rg16Sint = 17,
    /// Red and green channels. 16 bit integer per channel. [0, 65535] converted to/from float [0, 1] in shader.
    ///
    /// [`Features::TEXTURE_FORMAT_16BIT_NORM`] must be enabled to use this texture format.
    Rg16Unorm = 18,
    /// Red and green channels. 16 bit integer per channel. [0, 65535] converted to/from float [-1, 1] in shader.
    ///
    /// [`Features::TEXTURE_FORMAT_16BIT_NORM`] must be enabled to use this texture format.
    Rg16Snorm = 19,
    /// Red and green channels. 16 bit float per channel. Float in shader.
    Rg16Float = 20,
    /// Red, green, blue, and alpha channels. 8 bit integer per channel. [0, 255] converted to/from float [0, 1] in shader.
    Rgba8Unorm = 21,
    /// Red, green, blue, and alpha channels. 8 bit integer per channel. Srgb-color [0, 255] converted to/from linear-color float [0, 1] in shader.
    Rgba8UnormSrgb = 22,
    /// Red, green, blue, and alpha channels. 8 bit integer per channel. [-127, 127] converted to/from float [-1, 1] in shader.
    Rgba8Snorm = 23,
    /// Red, green, blue, and alpha channels. 8 bit integer per channel. Unsigned in shader.
    Rgba8Uint = 24,
    /// Red, green, blue, and alpha channels. 8 bit integer per channel. Signed in shader.
    Rgba8Sint = 25,
    /// Blue, green, red, and alpha channels. 8 bit integer per channel. [0, 255] converted to/from float [0, 1] in shader.
    Bgra8Unorm = 26,
    /// Blue, green, red, and alpha channels. 8 bit integer per channel. Srgb-color [0, 255] converted to/from linear-color float [0, 1] in shader.
    Bgra8UnormSrgb = 27,

    // Packed 32 bit formats
    /// Red, green, blue, and alpha channels. 10 bit integer for RGB channels, 2 bit integer for alpha channel. [0, 1023] ([0, 3] for alpha) converted to/from float [0, 1] in shader.
    Rgb10a2Unorm = 28,
    /// Red, green, and blue channels. 11 bit float with no sign bit for RG channels. 10 bit float with no sign bit for blue channel. Float in shader.
    Rg11b10Ufloat = 29,

    // Normal 64 bit formats
    /// Red and green channels. 32 bit integer per channel. Unsigned in shader.
    Rg32Uint = 30,
    /// Red and green channels. 32 bit integer per channel. Signed in shader.
    Rg32Sint = 31,
    /// Red and green channels. 32 bit float per channel. Float in shader.
    Rg32Float = 32,
    /// Red, green, blue, and alpha channels. 16 bit integer per channel. Unsigned in shader.
    Rgba16Uint = 33,
    /// Red, green, blue, and alpha channels. 16 bit integer per channel. Signed in shader.
    Rgba16Sint = 34,
    /// Red, green, blue, and alpha channels. 16 bit integer per channel. [0, 65535] converted to/from float [0, 1] in shader.
    ///
    /// [`Features::TEXTURE_FORMAT_16BIT_NORM`] must be enabled to use this texture format.
    Rgba16Unorm = 35,
    /// Red, green, blue, and alpha. 16 bit integer per channel. [0, 65535] converted to/from float [-1, 1] in shader.
    ///
    /// [`Features::TEXTURE_FORMAT_16BIT_NORM`] must be enabled to use this texture format.
    Rgba16Snorm = 36,
    /// Red, green, blue, and alpha channels. 16 bit float per channel. Float in shader.
    Rgba16Float = 37,

    // Normal 128 bit formats
    /// Red, green, blue, and alpha channels. 32 bit integer per channel. Unsigned in shader.
    Rgba32Uint = 38,
    /// Red, green, blue, and alpha channels. 32 bit integer per channel. Signed in shader.
    Rgba32Sint = 39,
    /// Red, green, blue, and alpha channels. 32 bit float per channel. Float in shader.
    Rgba32Float = 40,

    // Depth and stencil formats
    /// Special depth format with 32 bit floating point depth.
    Depth32Float = 41,
    /// Special depth/stencil format with 32 bit floating point depth and 8 bits integer stencil.
    Depth32FloatStencil8 = 42,
    /// Special depth format with at least 24 bit integer depth.
    Depth24Plus = 43,
    /// Special depth/stencil format with at least 24 bit integer depth and 8 bits integer stencil.
    Depth24PlusStencil8 = 44,
    /// Special depth/stencil format with 24 bit integer depth and 8 bits integer stencil.

    // Packed uncompressed texture formats
    /// Packed unsigned float with 9 bits mantisa for each RGB component, then a common 5 bits exponent
    Rgb9e5Ufloat = 45,

    // Compressed textures usable with `TEXTURE_COMPRESSION_BC` feature.
    /// 4x4 block compressed texture. 8 bytes per block (4 bit/px). 4 color + alpha pallet. 5 bit R + 6 bit G + 5 bit B + 1 bit alpha.
    /// [0, 63] ([0, 1] for alpha) converted to/from float [0, 1] in shader.
    ///
    /// Also known as DXT1.
    ///
    /// [`Features::TEXTURE_COMPRESSION_BC`] must be enabled to use this texture format.
    Bc1RgbaUnorm = 46,
    /// 4x4 block compressed texture. 8 bytes per block (4 bit/px). 4 color + alpha pallet. 5 bit R + 6 bit G + 5 bit B + 1 bit alpha.
    /// Srgb-color [0, 63] ([0, 1] for alpha) converted to/from linear-color float [0, 1] in shader.
    ///
    /// Also known as DXT1.
    ///
    /// [`Features::TEXTURE_COMPRESSION_BC`] must be enabled to use this texture format.
    Bc1RgbaUnormSrgb = 47,
    /// 4x4 block compressed texture. 16 bytes per block (8 bit/px). 4 color pallet. 5 bit R + 6 bit G + 5 bit B + 4 bit alpha.
    /// [0, 63] ([0, 15] for alpha) converted to/from float [0, 1] in shader.
    ///
    /// Also known as DXT3.
    ///
    /// [`Features::TEXTURE_COMPRESSION_BC`] must be enabled to use this texture format.
    Bc2RgbaUnorm = 48,
    /// 4x4 block compressed texture. 16 bytes per block (8 bit/px). 4 color pallet. 5 bit R + 6 bit G + 5 bit B + 4 bit alpha.
    /// Srgb-color [0, 63] ([0, 255] for alpha) converted to/from linear-color float [0, 1] in shader.
    ///
    /// Also known as DXT3.
    ///
    /// [`Features::TEXTURE_COMPRESSION_BC`] must be enabled to use this texture format.
    Bc2RgbaUnormSrgb = 49,
    /// 4x4 block compressed texture. 16 bytes per block (8 bit/px). 4 color pallet + 8 alpha pallet. 5 bit R + 6 bit G + 5 bit B + 8 bit alpha.
    /// [0, 63] ([0, 255] for alpha) converted to/from float [0, 1] in shader.
    ///
    /// Also known as DXT5.
    ///
    /// [`Features::TEXTURE_COMPRESSION_BC`] must be enabled to use this texture format.
    Bc3RgbaUnorm = 50,
    /// 4x4 block compressed texture. 16 bytes per block (8 bit/px). 4 color pallet + 8 alpha pallet. 5 bit R + 6 bit G + 5 bit B + 8 bit alpha.
    /// Srgb-color [0, 63] ([0, 255] for alpha) converted to/from linear-color float [0, 1] in shader.
    ///
    /// Also known as DXT5.
    ///
    /// [`Features::TEXTURE_COMPRESSION_BC`] must be enabled to use this texture format.
    Bc3RgbaUnormSrgb = 51,
    /// 4x4 block compressed texture. 8 bytes per block (4 bit/px). 8 color pallet. 8 bit R.
    /// [0, 255] converted to/from float [0, 1] in shader.
    ///
    /// Also known as RGTC1.
    ///
    /// [`Features::TEXTURE_COMPRESSION_BC`] must be enabled to use this texture format.
    Bc4RUnorm = 52,
    /// 4x4 block compressed texture. 8 bytes per block (4 bit/px). 8 color pallet. 8 bit R.
    /// [-127, 127] converted to/from float [-1, 1] in shader.
    ///
    /// Also known as RGTC1.
    ///
    /// [`Features::TEXTURE_COMPRESSION_BC`] must be enabled to use this texture format.
    Bc4RSnorm = 53,
    /// 4x4 block compressed texture. 16 bytes per block (8 bit/px). 8 color red pallet + 8 color green pallet. 8 bit RG.
    /// [0, 255] converted to/from float [0, 1] in shader.
    ///
    /// Also known as RGTC2.
    ///
    /// [`Features::TEXTURE_COMPRESSION_BC`] must be enabled to use this texture format.
    Bc5RgUnorm = 54,
    /// 4x4 block compressed texture. 16 bytes per block (8 bit/px). 8 color red pallet + 8 color green pallet. 8 bit RG.
    /// [-127, 127] converted to/from float [-1, 1] in shader.
    ///
    /// Also known as RGTC2.
    ///
    /// [`Features::TEXTURE_COMPRESSION_BC`] must be enabled to use this texture format.
    Bc5RgSnorm = 55,
    /// 4x4 block compressed texture. 16 bytes per block (8 bit/px). Variable sized pallet. 16 bit unsigned float RGB. Float in shader.
    ///
    /// Also known as BPTC (float).
    ///
    /// [`Features::TEXTURE_COMPRESSION_BC`] must be enabled to use this texture format.
    Bc6hRgbUfloat = 56,
    /// 4x4 block compressed texture. 16 bytes per block (8 bit/px). Variable sized pallet. 16 bit signed float RGB. Float in shader.
    ///
    /// Also known as BPTC (float).
    ///
    /// [`Features::TEXTURE_COMPRESSION_BC`] must be enabled to use this texture format.
    Bc6hRgbFloat = 57,
    /// 4x4 block compressed texture. 16 bytes per block (8 bit/px). Variable sized pallet. 8 bit integer RGBA.
    /// [0, 255] converted to/from float [0, 1] in shader.
    ///
    /// Also known as BPTC (unorm).
    ///
    /// [`Features::TEXTURE_COMPRESSION_BC`] must be enabled to use this texture format.
    Bc7RgbaUnorm = 58,
    /// 4x4 block compressed texture. 16 bytes per block (8 bit/px). Variable sized pallet. 8 bit integer RGBA.
    /// Srgb-color [0, 255] converted to/from linear-color float [0, 1] in shader.
    ///
    /// Also known as BPTC (unorm).
    ///
    /// [`Features::TEXTURE_COMPRESSION_BC`] must be enabled to use this texture format.
    Bc7RgbaUnormSrgb = 59,
    /// 4x4 block compressed texture. 8 bytes per block (4 bit/px). Complex pallet. 8 bit integer RGB.
    /// [0, 255] converted to/from float [0, 1] in shader.
    ///
    /// [`Features::TEXTURE_COMPRESSION_ETC2`] must be enabled to use this texture format.
    Etc2Rgb8Unorm = 60,
    /// 4x4 block compressed texture. 8 bytes per block (4 bit/px). Complex pallet. 8 bit integer RGB.
    /// Srgb-color [0, 255] converted to/from linear-color float [0, 1] in shader.
    ///
    /// [`Features::TEXTURE_COMPRESSION_ETC2`] must be enabled to use this texture format.
    Etc2Rgb8UnormSrgb = 61,
    /// 4x4 block compressed texture. 8 bytes per block (4 bit/px). Complex pallet. 8 bit integer RGB + 1 bit alpha.
    /// [0, 255] ([0, 1] for alpha) converted to/from float [0, 1] in shader.
    ///
    /// [`Features::TEXTURE_COMPRESSION_ETC2`] must be enabled to use this texture format.
    Etc2Rgb8A1Unorm = 62,
    /// 4x4 block compressed texture. 8 bytes per block (4 bit/px). Complex pallet. 8 bit integer RGB + 1 bit alpha.
    /// Srgb-color [0, 255] ([0, 1] for alpha) converted to/from linear-color float [0, 1] in shader.
    ///
    /// [`Features::TEXTURE_COMPRESSION_ETC2`] must be enabled to use this texture format.
    Etc2Rgb8A1UnormSrgb = 63,
    /// 4x4 block compressed texture. 16 bytes per block (8 bit/px). Complex pallet. 8 bit integer RGB + 8 bit alpha.
    /// [0, 255] converted to/from float [0, 1] in shader.
    ///
    /// [`Features::TEXTURE_COMPRESSION_ETC2`] must be enabled to use this texture format.
    Etc2Rgba8Unorm = 64,
    /// 4x4 block compressed texture. 16 bytes per block (8 bit/px). Complex pallet. 8 bit integer RGB + 8 bit alpha.
    /// Srgb-color [0, 255] converted to/from linear-color float [0, 1] in shader.
    ///
    /// [`Features::TEXTURE_COMPRESSION_ETC2`] must be enabled to use this texture format.
    Etc2Rgba8UnormSrgb = 65,
    /// 4x4 block compressed texture. 8 bytes per block (4 bit/px). Complex pallet. 11 bit integer R.
    /// [0, 255] converted to/from float [0, 1] in shader.
    ///
    /// [`Features::TEXTURE_COMPRESSION_ETC2`] must be enabled to use this texture format.
    EacR11Unorm = 66,
    /// 4x4 block compressed texture. 8 bytes per block (4 bit/px). Complex pallet. 11 bit integer R.
    /// [-127, 127] converted to/from float [-1, 1] in shader.
    ///
    /// [`Features::TEXTURE_COMPRESSION_ETC2`] must be enabled to use this texture format.
    EacR11Snorm = 67,
    /// 4x4 block compressed texture. 16 bytes per block (8 bit/px). Complex pallet. 11 bit integer R + 11 bit integer G.
    /// [0, 255] converted to/from float [0, 1] in shader.
    ///
    /// [`Features::TEXTURE_COMPRESSION_ETC2`] must be enabled to use this texture format.
    EacRg11Unorm = 68,
    /// 4x4 block compressed texture. 16 bytes per block (8 bit/px). Complex pallet. 11 bit integer R + 11 bit integer G.
    /// [-127, 127] converted to/from float [-1, 1] in shader.
    ///
    /// [`Features::TEXTURE_COMPRESSION_ETC2`] must be enabled to use this texture format.
    EacRg11Snorm = 69,
}

impl TextureFormat {
    pub const fn to_wgpu_type(&self) -> wgpu::TextureFormat {
        match self {
            Self::R8Unorm => wgpu::TextureFormat::R8Unorm,
            Self::R8Snorm => wgpu::TextureFormat::R8Snorm,
            Self::R8Uint => wgpu::TextureFormat::R8Uint,
            Self::R8Sint => wgpu::TextureFormat::R8Sint,
            Self::R16Uint => wgpu::TextureFormat::R16Uint,
            Self::R16Sint => wgpu::TextureFormat::R16Sint,
            Self::R16Unorm => wgpu::TextureFormat::R16Unorm,
            Self::R16Snorm => wgpu::TextureFormat::R16Snorm,
            Self::R16Float => wgpu::TextureFormat::R16Float,
            Self::Rg8Unorm => wgpu::TextureFormat::Rg8Unorm,
            Self::Rg8Snorm => wgpu::TextureFormat::Rg8Snorm,
            Self::Rg8Uint => wgpu::TextureFormat::Rg8Uint,
            Self::Rg8Sint => wgpu::TextureFormat::Rg8Sint,
            Self::R32Uint => wgpu::TextureFormat::R32Uint,
            Self::R32Sint => wgpu::TextureFormat::R32Sint,
            Self::R32Float => wgpu::TextureFormat::R32Float,
            Self::Rg16Uint => wgpu::TextureFormat::Rg16Uint,
            Self::Rg16Sint => wgpu::TextureFormat::Rg16Sint,
            Self::Rg16Unorm => wgpu::TextureFormat::Rg16Unorm,
            Self::Rg16Snorm => wgpu::TextureFormat::Rg16Snorm,
            Self::Rg16Float => wgpu::TextureFormat::Rg16Float,
            Self::Rgba8Unorm => wgpu::TextureFormat::Rgba8Unorm,
            Self::Rgba8UnormSrgb => wgpu::TextureFormat::Rgba8UnormSrgb,
            Self::Rgba8Snorm => wgpu::TextureFormat::Rgba8Snorm,
            Self::Rgba8Uint => wgpu::TextureFormat::Rgba8Uint,
            Self::Rgba8Sint => wgpu::TextureFormat::Rgba8Sint,
            Self::Bgra8Unorm => wgpu::TextureFormat::Bgra8Unorm,
            Self::Bgra8UnormSrgb => wgpu::TextureFormat::Bgra8UnormSrgb,
            Self::Rgb10a2Unorm => wgpu::TextureFormat::Rgb10a2Unorm,
            Self::Rg11b10Ufloat => wgpu::TextureFormat::Rg11b10Ufloat,
            Self::Rg32Uint => wgpu::TextureFormat::Rg32Uint,
            Self::Rg32Sint => wgpu::TextureFormat::Rg32Sint,
            Self::Rg32Float => wgpu::TextureFormat::Rg32Float,
            Self::Rgba16Uint => wgpu::TextureFormat::Rgba16Uint,
            Self::Rgba16Sint => wgpu::TextureFormat::Rgba16Sint,
            Self::Rgba16Unorm => wgpu::TextureFormat::Rgba16Unorm,
            Self::Rgba16Snorm => wgpu::TextureFormat::Rgba16Snorm,
            Self::Rgba16Float => wgpu::TextureFormat::Rgba16Float,
            Self::Rgba32Uint => wgpu::TextureFormat::Rgba32Uint,
            Self::Rgba32Sint => wgpu::TextureFormat::Rgba32Sint,
            Self::Rgba32Float => wgpu::TextureFormat::Rgba32Float,
            Self::Depth32Float => wgpu::TextureFormat::Depth32Float,
            Self::Depth32FloatStencil8 => wgpu::TextureFormat::Depth32FloatStencil8,
            Self::Depth24Plus => wgpu::TextureFormat::Depth24Plus,
            Self::Depth24PlusStencil8 => wgpu::TextureFormat::Depth24PlusStencil8,
            Self::Rgb9e5Ufloat => wgpu::TextureFormat::Rgb9e5Ufloat,
            Self::Bc1RgbaUnorm => wgpu::TextureFormat::Bc1RgbaUnorm,
            Self::Bc1RgbaUnormSrgb => wgpu::TextureFormat::Bc1RgbaUnormSrgb,
            Self::Bc2RgbaUnorm => wgpu::TextureFormat::Bc2RgbaUnorm,
            Self::Bc2RgbaUnormSrgb => wgpu::TextureFormat::Bc2RgbaUnormSrgb,
            Self::Bc3RgbaUnorm => wgpu::TextureFormat::Bc3RgbaUnorm,
            Self::Bc3RgbaUnormSrgb => wgpu::TextureFormat::Bc3RgbaUnormSrgb,
            Self::Bc4RUnorm => wgpu::TextureFormat::Bc4RUnorm,
            Self::Bc4RSnorm => wgpu::TextureFormat::Bc4RSnorm,
            Self::Bc5RgUnorm => wgpu::TextureFormat::Bc5RgUnorm,
            Self::Bc5RgSnorm => wgpu::TextureFormat::Bc5RgSnorm,
            Self::Bc6hRgbUfloat => wgpu::TextureFormat::Bc6hRgbUfloat,
            Self::Bc6hRgbFloat => wgpu::TextureFormat::Bc6hRgbFloat,
            Self::Bc7RgbaUnorm => wgpu::TextureFormat::Bc7RgbaUnorm,
            Self::Bc7RgbaUnormSrgb => wgpu::TextureFormat::Bc7RgbaUnormSrgb,
            Self::Etc2Rgb8Unorm => wgpu::TextureFormat::Etc2Rgb8Unorm,
            Self::Etc2Rgb8UnormSrgb => wgpu::TextureFormat::Etc2Rgb8UnormSrgb,
            Self::Etc2Rgb8A1Unorm => wgpu::TextureFormat::Etc2Rgb8A1Unorm,
            Self::Etc2Rgb8A1UnormSrgb => wgpu::TextureFormat::Etc2Rgb8A1UnormSrgb,
            Self::Etc2Rgba8Unorm => wgpu::TextureFormat::Etc2Rgba8Unorm,
            Self::Etc2Rgba8UnormSrgb => wgpu::TextureFormat::Etc2Rgba8UnormSrgb,
            Self::EacR11Unorm => wgpu::TextureFormat::EacR11Unorm,
            Self::EacR11Snorm => wgpu::TextureFormat::EacR11Snorm,
            Self::EacRg11Unorm => wgpu::TextureFormat::EacRg11Unorm,
            Self::EacRg11Snorm => wgpu::TextureFormat::EacRg11Snorm,
        }
    }
}

impl Default for TextureFormat {
    fn default() -> Self {
        Self::Rgba8UnormSrgb
    }
}

impl TryFrom<wgpu::TextureFormat> for TextureFormat {
    type Error = &'static str;

    fn try_from(value: wgpu::TextureFormat) -> Result<Self, Self::Error> {
        match value {
            wgpu::TextureFormat::R8Unorm => Ok(Self::R8Unorm),
            wgpu::TextureFormat::R8Snorm => Ok(Self::R8Snorm),
            wgpu::TextureFormat::R8Uint => Ok(Self::R8Uint),
            wgpu::TextureFormat::R8Sint => Ok(Self::R8Sint),
            wgpu::TextureFormat::R16Uint => Ok(Self::R16Uint),
            wgpu::TextureFormat::R16Sint => Ok(Self::R16Sint),
            wgpu::TextureFormat::R16Unorm => Ok(Self::R16Unorm),
            wgpu::TextureFormat::R16Snorm => Ok(Self::R16Snorm),
            wgpu::TextureFormat::R16Float => Ok(Self::R16Float),
            wgpu::TextureFormat::Rg8Unorm => Ok(Self::Rg8Unorm),
            wgpu::TextureFormat::Rg8Snorm => Ok(Self::Rg8Snorm),
            wgpu::TextureFormat::Rg8Uint => Ok(Self::Rg8Uint),
            wgpu::TextureFormat::Rg8Sint => Ok(Self::Rg8Sint),
            wgpu::TextureFormat::R32Uint => Ok(Self::R32Uint),
            wgpu::TextureFormat::R32Sint => Ok(Self::R32Sint),
            wgpu::TextureFormat::R32Float => Ok(Self::R32Float),
            wgpu::TextureFormat::Rg16Uint => Ok(Self::Rg16Uint),
            wgpu::TextureFormat::Rg16Sint => Ok(Self::Rg16Sint),
            wgpu::TextureFormat::Rg16Unorm => Ok(Self::Rg16Unorm),
            wgpu::TextureFormat::Rg16Snorm => Ok(Self::Rg16Snorm),
            wgpu::TextureFormat::Rg16Float => Ok(Self::Rg16Float),
            wgpu::TextureFormat::Rgba8Unorm => Ok(Self::Rgba8Unorm),
            wgpu::TextureFormat::Rgba8UnormSrgb => Ok(Self::Rgba8UnormSrgb),
            wgpu::TextureFormat::Rgba8Snorm => Ok(Self::Rgba8Snorm),
            wgpu::TextureFormat::Rgba8Uint => Ok(Self::Rgba8Uint),
            wgpu::TextureFormat::Rgba8Sint => Ok(Self::Rgba8Sint),
            wgpu::TextureFormat::Bgra8Unorm => Ok(Self::Bgra8Unorm),
            wgpu::TextureFormat::Bgra8UnormSrgb => Ok(Self::Bgra8UnormSrgb),
            wgpu::TextureFormat::Rgb10a2Unorm => Ok(Self::Rgb10a2Unorm),
            wgpu::TextureFormat::Rg11b10Ufloat => Ok(Self::Rg11b10Ufloat),
            wgpu::TextureFormat::Rg32Uint => Ok(Self::Rg32Uint),
            wgpu::TextureFormat::Rg32Sint => Ok(Self::Rg32Sint),
            wgpu::TextureFormat::Rg32Float => Ok(Self::Rg32Float),
            wgpu::TextureFormat::Rgba16Uint => Ok(Self::Rgba16Uint),
            wgpu::TextureFormat::Rgba16Sint => Ok(Self::Rgba16Sint),
            wgpu::TextureFormat::Rgba16Unorm => Ok(Self::Rgba16Unorm),
            wgpu::TextureFormat::Rgba16Snorm => Ok(Self::Rgba16Snorm),
            wgpu::TextureFormat::Rgba16Float => Ok(Self::Rgba16Float),
            wgpu::TextureFormat::Rgba32Uint => Ok(Self::Rgba32Uint),
            wgpu::TextureFormat::Rgba32Sint => Ok(Self::Rgba32Sint),
            wgpu::TextureFormat::Rgba32Float => Ok(Self::Rgba32Float),
            wgpu::TextureFormat::Depth32Float => Ok(Self::Depth32Float),
            wgpu::TextureFormat::Depth32FloatStencil8 => Ok(Self::Depth32FloatStencil8),
            wgpu::TextureFormat::Depth24Plus => Ok(Self::Depth24Plus),
            wgpu::TextureFormat::Depth24PlusStencil8 => Ok(Self::Depth24PlusStencil8),
            wgpu::TextureFormat::Rgb9e5Ufloat => Ok(Self::Rgb9e5Ufloat),
            wgpu::TextureFormat::Bc1RgbaUnorm => Ok(Self::Bc1RgbaUnorm),
            wgpu::TextureFormat::Bc1RgbaUnormSrgb => Ok(Self::Bc1RgbaUnormSrgb),
            wgpu::TextureFormat::Bc2RgbaUnorm => Ok(Self::Bc2RgbaUnorm),
            wgpu::TextureFormat::Bc2RgbaUnormSrgb => Ok(Self::Bc2RgbaUnormSrgb),
            wgpu::TextureFormat::Bc3RgbaUnorm => Ok(Self::Bc3RgbaUnorm),
            wgpu::TextureFormat::Bc3RgbaUnormSrgb => Ok(Self::Bc3RgbaUnormSrgb),
            wgpu::TextureFormat::Bc4RUnorm => Ok(Self::Bc4RUnorm),
            wgpu::TextureFormat::Bc4RSnorm => Ok(Self::Bc4RSnorm),
            wgpu::TextureFormat::Bc5RgUnorm => Ok(Self::Bc5RgUnorm),
            wgpu::TextureFormat::Bc5RgSnorm => Ok(Self::Bc5RgSnorm),
            wgpu::TextureFormat::Bc6hRgbUfloat => Ok(Self::Bc6hRgbUfloat),
            wgpu::TextureFormat::Bc6hRgbFloat => Ok(Self::Bc6hRgbFloat),
            wgpu::TextureFormat::Bc7RgbaUnorm => Ok(Self::Bc7RgbaUnorm),
            wgpu::TextureFormat::Bc7RgbaUnormSrgb => Ok(Self::Bc7RgbaUnormSrgb),
            wgpu::TextureFormat::Etc2Rgb8Unorm => Ok(Self::Etc2Rgb8Unorm),
            wgpu::TextureFormat::Etc2Rgb8UnormSrgb => Ok(Self::Etc2Rgb8UnormSrgb),
            wgpu::TextureFormat::Etc2Rgb8A1Unorm => Ok(Self::Etc2Rgb8A1Unorm),
            wgpu::TextureFormat::Etc2Rgb8A1UnormSrgb => Ok(Self::Etc2Rgb8A1UnormSrgb),
            wgpu::TextureFormat::Etc2Rgba8Unorm => Ok(Self::Etc2Rgba8Unorm),
            wgpu::TextureFormat::Etc2Rgba8UnormSrgb => Ok(Self::Etc2Rgba8UnormSrgb),
            wgpu::TextureFormat::EacR11Unorm => Ok(Self::EacR11Unorm),
            wgpu::TextureFormat::EacR11Snorm => Ok(Self::EacR11Snorm),
            wgpu::TextureFormat::EacRg11Unorm => Ok(Self::EacRg11Unorm),
            wgpu::TextureFormat::EacRg11Snorm => Ok(Self::EacRg11Snorm),
            _ => Err("not supported texture format"),
        }
    }
}

#[cfg(test)]
mod tests {
    #[test]
    #[rustfmt::skip]
    fn test_texture_format() {
        use crate::TextureFormat as A;
        use wgpu::TextureFormat as B;

        assert_eq!(A::R8Unorm.to_wgpu_type(), B::R8Unorm);
        assert_eq!(A::R8Snorm.to_wgpu_type(), B::R8Snorm);
        assert_eq!(A::R8Uint.to_wgpu_type(), B::R8Uint);
        assert_eq!(A::R8Sint.to_wgpu_type(), B::R8Sint);
        assert_eq!(A::R16Uint.to_wgpu_type(), B::R16Uint);
        assert_eq!(A::R16Sint.to_wgpu_type(), B::R16Sint);
        assert_eq!(A::R16Unorm.to_wgpu_type(), B::R16Unorm);
        assert_eq!(A::R16Snorm.to_wgpu_type(), B::R16Snorm);
        assert_eq!(A::R16Float.to_wgpu_type(), B::R16Float);
        assert_eq!(A::Rg8Unorm.to_wgpu_type(), B::Rg8Unorm);
        assert_eq!(A::Rg8Snorm.to_wgpu_type(), B::Rg8Snorm);
        assert_eq!(A::Rg8Uint.to_wgpu_type(), B::Rg8Uint);
        assert_eq!(A::Rg8Sint.to_wgpu_type(), B::Rg8Sint);
        assert_eq!(A::R32Uint.to_wgpu_type(), B::R32Uint);
        assert_eq!(A::R32Sint.to_wgpu_type(), B::R32Sint);
        assert_eq!(A::R32Float.to_wgpu_type(), B::R32Float);
        assert_eq!(A::Rg16Uint.to_wgpu_type(), B::Rg16Uint);
        assert_eq!(A::Rg16Sint.to_wgpu_type(), B::Rg16Sint);
        assert_eq!(A::Rg16Unorm.to_wgpu_type(), B::Rg16Unorm);
        assert_eq!(A::Rg16Snorm.to_wgpu_type(), B::Rg16Snorm);
        assert_eq!(A::Rg16Float.to_wgpu_type(), B::Rg16Float);
        assert_eq!(A::Rgba8Unorm.to_wgpu_type(), B::Rgba8Unorm);
        assert_eq!(A::Rgba8UnormSrgb.to_wgpu_type(), B::Rgba8UnormSrgb);
        assert_eq!(A::Rgba8Snorm.to_wgpu_type(), B::Rgba8Snorm);
        assert_eq!(A::Rgba8Uint.to_wgpu_type(), B::Rgba8Uint);
        assert_eq!(A::Rgba8Sint.to_wgpu_type(), B::Rgba8Sint);
        assert_eq!(A::Bgra8Unorm.to_wgpu_type(), B::Bgra8Unorm);
        assert_eq!(A::Bgra8UnormSrgb.to_wgpu_type(), B::Bgra8UnormSrgb);
        assert_eq!(A::Rgb10a2Unorm.to_wgpu_type(), B::Rgb10a2Unorm);
        assert_eq!(A::Rg11b10Ufloat.to_wgpu_type(), B::Rg11b10Ufloat);
        assert_eq!(A::Rg32Uint.to_wgpu_type(), B::Rg32Uint);
        assert_eq!(A::Rg32Sint.to_wgpu_type(), B::Rg32Sint);
        assert_eq!(A::Rg32Float.to_wgpu_type(), B::Rg32Float);
        assert_eq!(A::Rgba16Uint.to_wgpu_type(), B::Rgba16Uint);
        assert_eq!(A::Rgba16Sint.to_wgpu_type(), B::Rgba16Sint);
        assert_eq!(A::Rgba16Unorm.to_wgpu_type(), B::Rgba16Unorm);
        assert_eq!(A::Rgba16Snorm.to_wgpu_type(), B::Rgba16Snorm);
        assert_eq!(A::Rgba16Float.to_wgpu_type(), B::Rgba16Float);
        assert_eq!(A::Rgba32Uint.to_wgpu_type(), B::Rgba32Uint);
        assert_eq!(A::Rgba32Sint.to_wgpu_type(), B::Rgba32Sint);
        assert_eq!(A::Rgba32Float.to_wgpu_type(), B::Rgba32Float);
        assert_eq!(A::Depth32Float.to_wgpu_type(), B::Depth32Float);
        assert_eq!(A::Depth32FloatStencil8.to_wgpu_type(), B::Depth32FloatStencil8);
        assert_eq!(A::Depth24Plus.to_wgpu_type(), B::Depth24Plus);
        assert_eq!(A::Depth24PlusStencil8.to_wgpu_type(), B::Depth24PlusStencil8);
        assert_eq!(A::Rgb9e5Ufloat.to_wgpu_type(), B::Rgb9e5Ufloat);
        assert_eq!(A::Bc1RgbaUnorm.to_wgpu_type(), B::Bc1RgbaUnorm);
        assert_eq!(A::Bc1RgbaUnormSrgb.to_wgpu_type(), B::Bc1RgbaUnormSrgb);
        assert_eq!(A::Bc2RgbaUnorm.to_wgpu_type(), B::Bc2RgbaUnorm);
        assert_eq!(A::Bc2RgbaUnormSrgb.to_wgpu_type(), B::Bc2RgbaUnormSrgb);
        assert_eq!(A::Bc3RgbaUnorm.to_wgpu_type(), B::Bc3RgbaUnorm);
        assert_eq!(A::Bc3RgbaUnormSrgb.to_wgpu_type(), B::Bc3RgbaUnormSrgb);
        assert_eq!(A::Bc4RUnorm.to_wgpu_type(), B::Bc4RUnorm);
        assert_eq!(A::Bc4RSnorm.to_wgpu_type(), B::Bc4RSnorm);
        assert_eq!(A::Bc5RgUnorm.to_wgpu_type(), B::Bc5RgUnorm);
        assert_eq!(A::Bc5RgSnorm.to_wgpu_type(), B::Bc5RgSnorm);
        assert_eq!(A::Bc6hRgbUfloat.to_wgpu_type(), B::Bc6hRgbUfloat);
        assert_eq!(A::Bc6hRgbFloat.to_wgpu_type(), B::Bc6hRgbFloat);
        assert_eq!(A::Bc7RgbaUnorm.to_wgpu_type(), B::Bc7RgbaUnorm);
        assert_eq!(A::Bc7RgbaUnormSrgb.to_wgpu_type(), B::Bc7RgbaUnormSrgb);
        assert_eq!(A::Etc2Rgb8Unorm.to_wgpu_type(), B::Etc2Rgb8Unorm);
        assert_eq!(A::Etc2Rgb8UnormSrgb.to_wgpu_type(), B::Etc2Rgb8UnormSrgb);
        assert_eq!(A::Etc2Rgb8A1Unorm.to_wgpu_type(), B::Etc2Rgb8A1Unorm);
        assert_eq!(A::Etc2Rgb8A1UnormSrgb.to_wgpu_type(), B::Etc2Rgb8A1UnormSrgb);
        assert_eq!(A::Etc2Rgba8Unorm.to_wgpu_type(), B::Etc2Rgba8Unorm);
        assert_eq!(A::Etc2Rgba8UnormSrgb.to_wgpu_type(), B::Etc2Rgba8UnormSrgb);
        assert_eq!(A::EacR11Unorm.to_wgpu_type(), B::EacR11Unorm);
        assert_eq!(A::EacR11Snorm.to_wgpu_type(), B::EacR11Snorm);
        assert_eq!(A::EacRg11Unorm.to_wgpu_type(), B::EacRg11Unorm);
        assert_eq!(A::EacRg11Snorm.to_wgpu_type(), B::EacRg11Snorm);

    }

    #[test]
    #[rustfmt::skip]
    fn test_texture_format_from_wgpu() {
        use crate::TextureFormat as A;
        use wgpu::TextureFormat as B;

        assert_eq!(A::R8Unorm, B::R8Unorm.try_into().unwrap());
        assert_eq!(A::R8Snorm, B::R8Snorm.try_into().unwrap());
        assert_eq!(A::R8Uint, B::R8Uint.try_into().unwrap());
        assert_eq!(A::R8Sint, B::R8Sint.try_into().unwrap());
        assert_eq!(A::R16Uint, B::R16Uint.try_into().unwrap());
        assert_eq!(A::R16Sint, B::R16Sint.try_into().unwrap());
        assert_eq!(A::R16Unorm, B::R16Unorm.try_into().unwrap());
        assert_eq!(A::R16Snorm, B::R16Snorm.try_into().unwrap());
        assert_eq!(A::R16Float, B::R16Float.try_into().unwrap());
        assert_eq!(A::Rg8Unorm, B::Rg8Unorm.try_into().unwrap());
        assert_eq!(A::Rg8Snorm, B::Rg8Snorm.try_into().unwrap());
        assert_eq!(A::Rg8Uint, B::Rg8Uint.try_into().unwrap());
        assert_eq!(A::Rg8Sint, B::Rg8Sint.try_into().unwrap());
        assert_eq!(A::R32Uint, B::R32Uint.try_into().unwrap());
        assert_eq!(A::R32Sint, B::R32Sint.try_into().unwrap());
        assert_eq!(A::R32Float, B::R32Float.try_into().unwrap());
        assert_eq!(A::Rg16Uint, B::Rg16Uint.try_into().unwrap());
        assert_eq!(A::Rg16Sint, B::Rg16Sint.try_into().unwrap());
        assert_eq!(A::Rg16Unorm, B::Rg16Unorm.try_into().unwrap());
        assert_eq!(A::Rg16Snorm, B::Rg16Snorm.try_into().unwrap());
        assert_eq!(A::Rg16Float, B::Rg16Float.try_into().unwrap());
        assert_eq!(A::Rgba8Unorm, B::Rgba8Unorm.try_into().unwrap());
        assert_eq!(A::Rgba8UnormSrgb, B::Rgba8UnormSrgb.try_into().unwrap());
        assert_eq!(A::Rgba8Snorm, B::Rgba8Snorm.try_into().unwrap());
        assert_eq!(A::Rgba8Uint, B::Rgba8Uint.try_into().unwrap());
        assert_eq!(A::Rgba8Sint, B::Rgba8Sint.try_into().unwrap());
        assert_eq!(A::Bgra8Unorm, B::Bgra8Unorm.try_into().unwrap());
        assert_eq!(A::Bgra8UnormSrgb, B::Bgra8UnormSrgb.try_into().unwrap());
        assert_eq!(A::Rgb10a2Unorm, B::Rgb10a2Unorm.try_into().unwrap());
        assert_eq!(A::Rg11b10Ufloat, B::Rg11b10Ufloat.try_into().unwrap());
        assert_eq!(A::Rg32Uint, B::Rg32Uint.try_into().unwrap());
        assert_eq!(A::Rg32Sint, B::Rg32Sint.try_into().unwrap());
        assert_eq!(A::Rg32Float, B::Rg32Float.try_into().unwrap());
        assert_eq!(A::Rgba16Uint, B::Rgba16Uint.try_into().unwrap());
        assert_eq!(A::Rgba16Sint, B::Rgba16Sint.try_into().unwrap());
        assert_eq!(A::Rgba16Unorm, B::Rgba16Unorm.try_into().unwrap());
        assert_eq!(A::Rgba16Snorm, B::Rgba16Snorm.try_into().unwrap());
        assert_eq!(A::Rgba16Float, B::Rgba16Float.try_into().unwrap());
        assert_eq!(A::Rgba32Uint, B::Rgba32Uint.try_into().unwrap());
        assert_eq!(A::Rgba32Sint, B::Rgba32Sint.try_into().unwrap());
        assert_eq!(A::Rgba32Float, B::Rgba32Float.try_into().unwrap());
        assert_eq!(A::Depth32Float, B::Depth32Float.try_into().unwrap());
        assert_eq!(A::Depth32FloatStencil8, B::Depth32FloatStencil8.try_into().unwrap());
        assert_eq!(A::Depth24Plus, B::Depth24Plus.try_into().unwrap());
        assert_eq!(A::Depth24PlusStencil8, B::Depth24PlusStencil8.try_into().unwrap());
        assert_eq!(A::Rgb9e5Ufloat, B::Rgb9e5Ufloat.try_into().unwrap());
        assert_eq!(A::Bc1RgbaUnorm, B::Bc1RgbaUnorm.try_into().unwrap());
        assert_eq!(A::Bc1RgbaUnormSrgb, B::Bc1RgbaUnormSrgb.try_into().unwrap());
        assert_eq!(A::Bc2RgbaUnorm, B::Bc2RgbaUnorm.try_into().unwrap());
        assert_eq!(A::Bc2RgbaUnormSrgb, B::Bc2RgbaUnormSrgb.try_into().unwrap());
        assert_eq!(A::Bc3RgbaUnorm, B::Bc3RgbaUnorm.try_into().unwrap());
        assert_eq!(A::Bc3RgbaUnormSrgb, B::Bc3RgbaUnormSrgb.try_into().unwrap());
        assert_eq!(A::Bc4RUnorm, B::Bc4RUnorm.try_into().unwrap());
        assert_eq!(A::Bc4RSnorm, B::Bc4RSnorm.try_into().unwrap());
        assert_eq!(A::Bc5RgUnorm, B::Bc5RgUnorm.try_into().unwrap());
        assert_eq!(A::Bc5RgSnorm, B::Bc5RgSnorm.try_into().unwrap());
        assert_eq!(A::Bc6hRgbUfloat, B::Bc6hRgbUfloat.try_into().unwrap());
        assert_eq!(A::Bc6hRgbFloat, B::Bc6hRgbFloat.try_into().unwrap());
        assert_eq!(A::Bc7RgbaUnorm, B::Bc7RgbaUnorm.try_into().unwrap());
        assert_eq!(A::Bc7RgbaUnormSrgb, B::Bc7RgbaUnormSrgb.try_into().unwrap());
        assert_eq!(A::Etc2Rgb8Unorm, B::Etc2Rgb8Unorm.try_into().unwrap());
        assert_eq!(A::Etc2Rgb8UnormSrgb, B::Etc2Rgb8UnormSrgb.try_into().unwrap());
        assert_eq!(A::Etc2Rgb8A1Unorm, B::Etc2Rgb8A1Unorm.try_into().unwrap());
        assert_eq!(A::Etc2Rgb8A1UnormSrgb, B::Etc2Rgb8A1UnormSrgb.try_into().unwrap());
        assert_eq!(A::Etc2Rgba8Unorm, B::Etc2Rgba8Unorm.try_into().unwrap());
        assert_eq!(A::Etc2Rgba8UnormSrgb, B::Etc2Rgba8UnormSrgb.try_into().unwrap());
        assert_eq!(A::EacR11Unorm, B::EacR11Unorm.try_into().unwrap());
        assert_eq!(A::EacR11Snorm, B::EacR11Snorm.try_into().unwrap());
        assert_eq!(A::EacRg11Unorm, B::EacRg11Unorm.try_into().unwrap());
        assert_eq!(A::EacRg11Snorm, B::EacRg11Snorm.try_into().unwrap());

    }
}

#[repr(C)]
#[derive(Debug, Clone, Copy)]
pub(crate) struct TextureFormatFeatures {
    pub allowed_usages: wgpu::TextureUsages,
    pub flags: wgpu::TextureFormatFeatureFlags,
}

impl Default for TextureFormatFeatures {
    fn default() -> Self {
        Self {
            allowed_usages: wgpu::TextureUsages::empty(),
            flags: wgpu::TextureFormatFeatureFlags::empty(),
        }
    }
}

impl From<wgpu::TextureFormatFeatures> for TextureFormatFeatures {
    fn from(value: wgpu::TextureFormatFeatures) -> Self {
        Self {
            allowed_usages: value.allowed_usages,
            flags: value.flags,
        }
    }
}

#[repr(C)]
#[derive(Debug)]
pub(crate) struct TextureDescriptor {
    pub size: wgpu::Extent3d,
    pub mip_level_count: u32,
    pub sample_count: u32,
    pub dimension: TextureDimension,
    pub format: TextureFormat,
    pub usage: wgpu::TextureUsages,
}

impl TryFrom<wgpu::TextureDescriptor<'_>> for TextureDescriptor {
    type Error = &'static str;

    fn try_from(value: wgpu::TextureDescriptor<'_>) -> Result<Self, Self::Error> {
        Ok(Self {
            size: value.size,
            mip_level_count: value.mip_level_count,
            sample_count: value.sample_count,
            dimension: value.dimension.into(),
            format: value.format.try_into()?,
            usage: value.usage,
        })
    }
}

impl TextureDescriptor {
    pub const fn to_wgpu_type(&self) -> wgpu::TextureDescriptor<'static> {
        assert!(self.size.width > 0);
        assert!(self.size.height > 0);
        assert!(self.size.depth_or_array_layers > 0);
        assert!(self.sample_count > 0);
        wgpu::TextureDescriptor {
            label: None,
            size: self.size,
            mip_level_count: self.mip_level_count,
            sample_count: self.sample_count,
            dimension: self.dimension.to_wgpu_type(),
            format: self.format.to_wgpu_type(),
            usage: self.usage,
            view_formats: &[],
        }
    }
}

#[repr(u32)]
#[derive(Debug, PartialEq, Eq, Clone, Copy)]
#[allow(dead_code)] // because values are from FFI
pub(crate) enum TextureDimension {
    D1 = 0,
    D2 = 1,
    D3 = 2,
}

impl From<wgpu::TextureDimension> for TextureDimension {
    fn from(value: wgpu::TextureDimension) -> Self {
        match value {
            wgpu::TextureDimension::D1 => Self::D1,
            wgpu::TextureDimension::D2 => Self::D2,
            wgpu::TextureDimension::D3 => Self::D3,
        }
    }
}

impl TextureDimension {
    pub const fn to_wgpu_type(&self) -> wgpu::TextureDimension {
        match self {
            Self::D1 => wgpu::TextureDimension::D1,
            Self::D2 => wgpu::TextureDimension::D2,
            Self::D3 => wgpu::TextureDimension::D3,
        }
    }
}

#[repr(C)]
pub(crate) struct VertexBufferLayout<'a> {
    pub array_stride: u64,
    pub step_mode: wgpu::VertexStepMode,
    pub attributes: Slice<'a, wgpu::VertexAttribute>,
}

impl<'a> VertexBufferLayout<'a> {
    pub fn to_wgpu_type(&self) -> wgpu::VertexBufferLayout {
        wgpu::VertexBufferLayout {
            array_stride: self.array_stride,
            step_mode: self.step_mode,
            attributes: &self.attributes,
        }
    }
}

#[repr(C)]
#[derive(Default, Clone, Copy)]
pub(crate) struct SizeU32 {
    pub width: u32,
    pub height: u32,
}

impl From<(u32, u32)> for SizeU32 {
    fn from(value: (u32, u32)) -> Self {
        Self {
            width: value.0,
            height: value.1,
        }
    }
}

#[repr(C)]
#[derive(Default)]
pub(crate) struct Tuple<T0, T1> {
    pub v0: T0,
    pub v1: T1,
}

impl<T0: Copy, T1: Copy> Tuple<T0, T1> {
    #[allow(dead_code)]
    pub const fn to_tuple(&self) -> (T0, T1) {
        (self.v0, self.v1)
    }
}

impl<T1, T2> From<(T1, T2)> for Tuple<T1, T2> {
    fn from(value: (T1, T2)) -> Self {
        Self {
            v0: value.0,
            v1: value.1,
        }
    }
}

/// ffi-safe `Option<T>`
/// (use `Option<T>` if T is reference)
#[repr(C)]
#[derive(Debug)]
pub(crate) struct Opt<T> {
    exists: bool,
    value: mem::MaybeUninit<T>,
}

impl<T> Default for Opt<T> {
    fn default() -> Self {
        Self::none()
    }
}

impl<T> Opt<T> {
    pub const fn some(value: T) -> Self {
        Self {
            exists: true,
            value: mem::MaybeUninit::new(value),
        }
    }

    pub const fn none() -> Self {
        Self {
            exists: false,
            value: mem::MaybeUninit::uninit(),
        }
    }

    pub fn map_to_option<U, F>(&self, f: F) -> Option<U>
    where
        F: FnOnce(&T) -> U,
    {
        match self.exists {
            true => Some(f(unsafe { self.value.assume_init_ref() })),
            false => None,
        }
    }
}

impl<T> From<Option<T>> for Opt<T> {
    fn from(o: Option<T>) -> Self {
        match o {
            Some(x) => Opt::some(x),
            None => Opt::none(),
        }
    }
}

impl<T> Opt<T> {
    pub const fn to_ref_option(&self) -> Option<&T> {
        match self.exists {
            true => Some(unsafe { self.value.assume_init_ref() }),
            false => None,
        }
    }
}

impl<T: Clone> Opt<T> {
    pub fn to_option(&self) -> Option<T> {
        match self.exists {
            true => Some(unsafe { self.value.assume_init_ref() }.clone()),
            false => None,
        }
    }
}

#[repr(C)]
#[derive(Debug)]
pub(crate) struct BufferSlice<'a> {
    pub buffer: &'a wgpu::Buffer,
    pub range: RangeBoundsU64,
}

impl<'a> BufferSlice<'a> {
    pub fn to_wgpu_type(&self) -> wgpu::BufferSlice<'a> {
        self.buffer.slice(self.range)
    }
}

#[repr(C)]
#[derive(Clone, Copy, Debug)]
pub(crate) struct Slice<'a, T> {
    pub data: Option<&'a T>,
    pub len: usize,
}

impl<'a, T> Default for Slice<'a, T> {
    fn default() -> Self {
        Self::empty()
    }
}

impl<'a, T> ops::Deref for Slice<'a, T> {
    type Target = [T];

    fn deref(&self) -> &Self::Target {
        self.as_slice()
    }
}

impl<'a, T> Slice<'a, T> {
    #[inline]
    pub fn as_slice(&self) -> &'a [T] {
        match self.len {
            0 => &[],
            _ => unsafe {
                let r: &T = self.data.unwrap();
                let p: *const T = r;
                std::slice::from_raw_parts(p, self.len)
            },
        }
    }

    pub const fn empty() -> Self {
        Self { data: None, len: 0 }
    }

    #[inline]
    pub fn new(s: &'a [T]) -> Self {
        Self {
            data: s.get(0),
            len: s.len(),
        }
    }

    #[inline]
    pub fn iter(&self) -> std::slice::Iter<T> {
        self.as_slice().iter()
    }
}

impl Slice<'_, u8> {
    #[inline]
    pub fn as_str(&self) -> Result<&str, str::Utf8Error> {
        std::str::from_utf8(self)
    }
}

#[repr(C)]
#[derive(Clone, Copy, Debug)]
pub(crate) struct RangeU32 {
    pub start: u32,
    pub end_excluded: u32,
}

impl RangeU32 {
    pub fn to_range(&self) -> ops::Range<u32> {
        ops::Range {
            start: self.start,
            end: self.end_excluded,
        }
    }
}

#[repr(C)]
#[derive(Clone, Copy, Debug)]
pub(crate) struct RangeBoundsU64 {
    pub start: u64,
    pub end_excluded: u64,
    pub has_start: bool,
    pub has_end_excluded: bool,
}

impl ops::RangeBounds<u64> for RangeBoundsU64 {
    fn start_bound(&self) -> ops::Bound<&u64> {
        if self.has_start {
            ops::Bound::Included(&self.start)
        } else {
            ops::Bound::Unbounded
        }
    }

    fn end_bound(&self) -> ops::Bound<&u64> {
        if self.has_end_excluded {
            ops::Bound::Excluded(&self.end_excluded)
        } else {
            ops::Bound::Unbounded
        }
    }
}

#[repr(C)]
pub(crate) struct ImeInputData<'a> {
    tag: ImeInputDataTag,
    text: Slice<'a, u8>,
    range: Opt<(usize, usize)>,
}

impl<'a> From<&'a Ime> for ImeInputData<'a> {
    fn from(value: &'a Ime) -> Self {
        Self::new(value)
    }
}

impl<'a> ImeInputData<'a> {
    fn new(value: &'a Ime) -> Self {
        match value {
            Ime::Enabled => Self {
                tag: ImeInputDataTag::Enabled,
                text: Slice::default(),
                range: None.into(),
            },
            Ime::Preedit(text, range) => Self {
                tag: ImeInputDataTag::Preedit,
                text: Slice::new(text.as_bytes()),
                range: range.clone().into(),
            },
            Ime::Commit(text) => Self {
                tag: ImeInputDataTag::Commit,
                text: Slice::new(text.as_bytes()),
                range: None.into(),
            },
            Ime::Disabled => Self {
                tag: ImeInputDataTag::Disabled,
                text: Slice::default(),
                range: None.into(),
            },
        }
    }
}

#[repr(u32)]
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub(crate) enum ImeInputDataTag {
    Enabled = 0,
    Preedit = 1,
    Commit = 2,
    Disabled = 3,
}

#[repr(C)]
pub(crate) struct ScreenInfo {
    pub backend: wgpu::Backend,
    pub surface_format: Opt<TextureFormat>,
}

// To be used as a value type without release outside of Rust, the following conditions must be met.
// - The type does not implement `Drop` trait
// - The type has `'static` lifetime
// - The type implements `Sized` trait
static_assertions::assert_eq_size!(window::WindowId, usize);
static_assertions::assert_not_impl_any!(window::WindowId: Drop);

static_assertions::assert_eq_size!(winit::monitor::MonitorHandle, usize);
static_assertions::assert_not_impl_any!(winit::monitor::MonitorHandle: Drop);
static_assertions::assert_eq_size!(MonitorId, winit::monitor::MonitorHandle);
static_assertions::assert_not_impl_any!(MonitorId: Drop);

#[repr(C)]
#[derive(Debug, Clone, PartialEq, Eq)]
pub(crate) struct MonitorId {
    monitor: usize,
    _ghost: std::marker::PhantomData<winit::monitor::MonitorHandle>,
}

impl MonitorId {
    pub fn new(monitor: winit::monitor::MonitorHandle) -> Self {
        Self {
            monitor: unsafe { std::mem::transmute(monitor) },
            _ghost: Default::default(),
        }
    }

    pub fn monitor(&self) -> winit::monitor::MonitorHandle {
        unsafe { std::mem::transmute(self.monitor) }
    }
}

#[repr(C)]
#[derive(Debug, PartialEq, Eq)]
pub(crate) struct MouseButton {
    /// the button number.
    /// 0: left, 1: right, 2: middle (when `is_named_buton` is true)
    number: u16,
    is_named_buton: bool,
}

impl From<winit::event::MouseButton> for MouseButton {
    fn from(value: winit::event::MouseButton) -> Self {
        match value {
            winit::event::MouseButton::Left => MouseButton {
                number: 0,
                is_named_buton: true,
            },
            winit::event::MouseButton::Right => MouseButton {
                number: 1,
                is_named_buton: true,
            },
            winit::event::MouseButton::Middle => MouseButton {
                number: 2,
                is_named_buton: true,
            },
            winit::event::MouseButton::Back => MouseButton {
                number: 3,
                is_named_buton: true,
            },
            winit::event::MouseButton::Forward => MouseButton {
                number: 4,
                is_named_buton: true,
            },
            winit::event::MouseButton::Other(n) => MouseButton {
                number: n,
                is_named_buton: false,
            },
        }
    }
}

#[repr(u32)]
#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
pub enum KeyCode {
    Backquote = 0,
    Backslash = 1,
    BracketLeft = 2,
    BracketRight = 3,
    Comma = 4,
    Digit0 = 5,
    Digit1 = 6,
    Digit2 = 7,
    Digit3 = 8,
    Digit4 = 9,
    Digit5 = 10,
    Digit6 = 11,
    Digit7 = 12,
    Digit8 = 13,
    Digit9 = 14,
    Equal = 15,
    IntlBackslash = 16,
    IntlRo = 17,
    IntlYen = 18,
    KeyA = 19,
    KeyB = 20,
    KeyC = 21,
    KeyD = 22,
    KeyE = 23,
    KeyF = 24,
    KeyG = 25,
    KeyH = 26,
    KeyI = 27,
    KeyJ = 28,
    KeyK = 29,
    KeyL = 30,
    KeyM = 31,
    KeyN = 32,
    KeyO = 33,
    KeyP = 34,
    KeyQ = 35,
    KeyR = 36,
    KeyS = 37,
    KeyT = 38,
    KeyU = 39,
    KeyV = 40,
    KeyW = 41,
    KeyX = 42,
    KeyY = 43,
    KeyZ = 44,
    Minus = 45,
    Period = 46,
    Quote = 47,
    Semicolon = 48,
    Slash = 49,
    AltLeft = 50,
    AltRight = 51,
    Backspace = 52,
    CapsLock = 53,
    ContextMenu = 54,
    ControlLeft = 55,
    ControlRight = 56,
    Enter = 57,
    SuperLeft = 58,
    SuperRight = 59,
    ShiftLeft = 60,
    ShiftRight = 61,
    Space = 62,
    Tab = 63,
    Convert = 64,
    KanaMode = 65,
    Lang1 = 66,
    Lang2 = 67,
    Lang3 = 68,
    Lang4 = 69,
    Lang5 = 70,
    NonConvert = 71,
    Delete = 72,
    End = 73,
    Help = 74,
    Home = 75,
    Insert = 76,
    PageDown = 77,
    PageUp = 78,
    ArrowDown = 79,
    ArrowLeft = 80,
    ArrowRight = 81,
    ArrowUp = 82,
    NumLock = 83,
    Numpad0 = 84,
    Numpad1 = 85,
    Numpad2 = 86,
    Numpad3 = 87,
    Numpad4 = 88,
    Numpad5 = 89,
    Numpad6 = 90,
    Numpad7 = 91,
    Numpad8 = 92,
    Numpad9 = 93,
    NumpadAdd = 94,
    NumpadBackspace = 95,
    NumpadClear = 96,
    NumpadClearEntry = 97,
    NumpadComma = 98,
    NumpadDecimal = 99,
    NumpadDivide = 100,
    NumpadEnter = 101,
    NumpadEqual = 102,
    NumpadHash = 103,
    NumpadMemoryAdd = 104,
    NumpadMemoryClear = 105,
    NumpadMemoryRecall = 106,
    NumpadMemoryStore = 107,
    NumpadMemorySubtract = 108,
    NumpadMultiply = 109,
    NumpadParenLeft = 110,
    NumpadParenRight = 111,
    NumpadStar = 112,
    NumpadSubtract = 113,
    Escape = 114,
    Fn = 115,
    FnLock = 116,
    PrintScreen = 117,
    ScrollLock = 118,
    Pause = 119,
    BrowserBack = 120,
    BrowserFavorites = 121,
    BrowserForward = 122,
    BrowserHome = 123,
    BrowserRefresh = 124,
    BrowserSearch = 125,
    BrowserStop = 126,
    Eject = 127,
    LaunchApp1 = 128,
    LaunchApp2 = 129,
    LaunchMail = 130,
    MediaPlayPause = 131,
    MediaSelect = 132,
    MediaStop = 133,
    MediaTrackNext = 134,
    MediaTrackPrevious = 135,
    Power = 136,
    Sleep = 137,
    AudioVolumeDown = 138,
    AudioVolumeMute = 139,
    AudioVolumeUp = 140,
    WakeUp = 141,
    Meta = 142,
    Hyper = 143,
    Turbo = 144,
    Abort = 145,
    Resume = 146,
    Suspend = 147,
    Again = 148,
    Copy = 149,
    Cut = 150,
    Find = 151,
    Open = 152,
    Paste = 153,
    Props = 154,
    Select = 155,
    Undo = 156,
    Hiragana = 157,
    Katakana = 158,
    F1 = 159,
    F2 = 160,
    F3 = 161,
    F4 = 162,
    F5 = 163,
    F6 = 164,
    F7 = 165,
    F8 = 166,
    F9 = 167,
    F10 = 168,
    F11 = 169,
    F12 = 170,
    F13 = 171,
    F14 = 172,
    F15 = 173,
    F16 = 174,
    F17 = 175,
    F18 = 176,
    F19 = 177,
    F20 = 178,
    F21 = 179,
    F22 = 180,
    F23 = 181,
    F24 = 182,
    F25 = 183,
    F26 = 184,
    F27 = 185,
    F28 = 186,
    F29 = 187,
    F30 = 188,
    F31 = 189,
    F32 = 190,
    F33 = 191,
    F34 = 192,
    F35 = 193,
}

impl TryFrom<winit::keyboard::KeyCode> for KeyCode {
    type Error = winit::keyboard::KeyCode;

    fn try_from(value: winit::keyboard::KeyCode) -> Result<Self, Self::Error> {
        (&value).try_into()
    }
}

impl TryFrom<&winit::keyboard::KeyCode> for KeyCode {
    type Error = winit::keyboard::KeyCode;

    fn try_from(value: &winit::keyboard::KeyCode) -> Result<Self, Self::Error> {
        use winit::keyboard::KeyCode as WKC;
        match value {
            WKC::Backquote => Ok(Self::Backquote),
            WKC::Backslash => Ok(Self::Backslash),
            WKC::BracketLeft => Ok(Self::BracketLeft),
            WKC::BracketRight => Ok(Self::BracketRight),
            WKC::Comma => Ok(Self::Comma),
            WKC::Digit0 => Ok(Self::Digit0),
            WKC::Digit1 => Ok(Self::Digit1),
            WKC::Digit2 => Ok(Self::Digit2),
            WKC::Digit3 => Ok(Self::Digit3),
            WKC::Digit4 => Ok(Self::Digit4),
            WKC::Digit5 => Ok(Self::Digit5),
            WKC::Digit6 => Ok(Self::Digit6),
            WKC::Digit7 => Ok(Self::Digit7),
            WKC::Digit8 => Ok(Self::Digit8),
            WKC::Digit9 => Ok(Self::Digit9),
            WKC::Equal => Ok(Self::Equal),
            WKC::IntlBackslash => Ok(Self::IntlBackslash),
            WKC::IntlRo => Ok(Self::IntlRo),
            WKC::IntlYen => Ok(Self::IntlYen),
            WKC::KeyA => Ok(Self::KeyA),
            WKC::KeyB => Ok(Self::KeyB),
            WKC::KeyC => Ok(Self::KeyC),
            WKC::KeyD => Ok(Self::KeyD),
            WKC::KeyE => Ok(Self::KeyE),
            WKC::KeyF => Ok(Self::KeyF),
            WKC::KeyG => Ok(Self::KeyG),
            WKC::KeyH => Ok(Self::KeyH),
            WKC::KeyI => Ok(Self::KeyI),
            WKC::KeyJ => Ok(Self::KeyJ),
            WKC::KeyK => Ok(Self::KeyK),
            WKC::KeyL => Ok(Self::KeyL),
            WKC::KeyM => Ok(Self::KeyM),
            WKC::KeyN => Ok(Self::KeyN),
            WKC::KeyO => Ok(Self::KeyO),
            WKC::KeyP => Ok(Self::KeyP),
            WKC::KeyQ => Ok(Self::KeyQ),
            WKC::KeyR => Ok(Self::KeyR),
            WKC::KeyS => Ok(Self::KeyS),
            WKC::KeyT => Ok(Self::KeyT),
            WKC::KeyU => Ok(Self::KeyU),
            WKC::KeyV => Ok(Self::KeyV),
            WKC::KeyW => Ok(Self::KeyW),
            WKC::KeyX => Ok(Self::KeyX),
            WKC::KeyY => Ok(Self::KeyY),
            WKC::KeyZ => Ok(Self::KeyZ),
            WKC::Minus => Ok(Self::Minus),
            WKC::Period => Ok(Self::Period),
            WKC::Quote => Ok(Self::Quote),
            WKC::Semicolon => Ok(Self::Semicolon),
            WKC::Slash => Ok(Self::Slash),
            WKC::AltLeft => Ok(Self::AltLeft),
            WKC::AltRight => Ok(Self::AltRight),
            WKC::Backspace => Ok(Self::Backspace),
            WKC::CapsLock => Ok(Self::CapsLock),
            WKC::ContextMenu => Ok(Self::ContextMenu),
            WKC::ControlLeft => Ok(Self::ControlLeft),
            WKC::ControlRight => Ok(Self::ControlRight),
            WKC::Enter => Ok(Self::Enter),
            WKC::SuperLeft => Ok(Self::SuperLeft),
            WKC::SuperRight => Ok(Self::SuperRight),
            WKC::ShiftLeft => Ok(Self::ShiftLeft),
            WKC::ShiftRight => Ok(Self::ShiftRight),
            WKC::Space => Ok(Self::Space),
            WKC::Tab => Ok(Self::Tab),
            WKC::Convert => Ok(Self::Convert),
            WKC::KanaMode => Ok(Self::KanaMode),
            WKC::Lang1 => Ok(Self::Lang1),
            WKC::Lang2 => Ok(Self::Lang2),
            WKC::Lang3 => Ok(Self::Lang3),
            WKC::Lang4 => Ok(Self::Lang4),
            WKC::Lang5 => Ok(Self::Lang5),
            WKC::NonConvert => Ok(Self::NonConvert),
            WKC::Delete => Ok(Self::Delete),
            WKC::End => Ok(Self::End),
            WKC::Help => Ok(Self::Help),
            WKC::Home => Ok(Self::Home),
            WKC::Insert => Ok(Self::Insert),
            WKC::PageDown => Ok(Self::PageDown),
            WKC::PageUp => Ok(Self::PageUp),
            WKC::ArrowDown => Ok(Self::ArrowDown),
            WKC::ArrowLeft => Ok(Self::ArrowLeft),
            WKC::ArrowRight => Ok(Self::ArrowRight),
            WKC::ArrowUp => Ok(Self::ArrowUp),
            WKC::NumLock => Ok(Self::NumLock),
            WKC::Numpad0 => Ok(Self::Numpad0),
            WKC::Numpad1 => Ok(Self::Numpad1),
            WKC::Numpad2 => Ok(Self::Numpad2),
            WKC::Numpad3 => Ok(Self::Numpad3),
            WKC::Numpad4 => Ok(Self::Numpad4),
            WKC::Numpad5 => Ok(Self::Numpad5),
            WKC::Numpad6 => Ok(Self::Numpad6),
            WKC::Numpad7 => Ok(Self::Numpad7),
            WKC::Numpad8 => Ok(Self::Numpad8),
            WKC::Numpad9 => Ok(Self::Numpad9),
            WKC::NumpadAdd => Ok(Self::NumpadAdd),
            WKC::NumpadBackspace => Ok(Self::NumpadBackspace),
            WKC::NumpadClear => Ok(Self::NumpadClear),
            WKC::NumpadClearEntry => Ok(Self::NumpadClearEntry),
            WKC::NumpadComma => Ok(Self::NumpadComma),
            WKC::NumpadDecimal => Ok(Self::NumpadDecimal),
            WKC::NumpadDivide => Ok(Self::NumpadDivide),
            WKC::NumpadEnter => Ok(Self::NumpadEnter),
            WKC::NumpadEqual => Ok(Self::NumpadEqual),
            WKC::NumpadHash => Ok(Self::NumpadHash),
            WKC::NumpadMemoryAdd => Ok(Self::NumpadMemoryAdd),
            WKC::NumpadMemoryClear => Ok(Self::NumpadMemoryClear),
            WKC::NumpadMemoryRecall => Ok(Self::NumpadMemoryRecall),
            WKC::NumpadMemoryStore => Ok(Self::NumpadMemoryStore),
            WKC::NumpadMemorySubtract => Ok(Self::NumpadMemorySubtract),
            WKC::NumpadMultiply => Ok(Self::NumpadMultiply),
            WKC::NumpadParenLeft => Ok(Self::NumpadParenLeft),
            WKC::NumpadParenRight => Ok(Self::NumpadParenRight),
            WKC::NumpadStar => Ok(Self::NumpadStar),
            WKC::NumpadSubtract => Ok(Self::NumpadSubtract),
            WKC::Escape => Ok(Self::Escape),
            WKC::Fn => Ok(Self::Fn),
            WKC::FnLock => Ok(Self::FnLock),
            WKC::PrintScreen => Ok(Self::PrintScreen),
            WKC::ScrollLock => Ok(Self::ScrollLock),
            WKC::Pause => Ok(Self::Pause),
            WKC::BrowserBack => Ok(Self::BrowserBack),
            WKC::BrowserFavorites => Ok(Self::BrowserFavorites),
            WKC::BrowserForward => Ok(Self::BrowserForward),
            WKC::BrowserHome => Ok(Self::BrowserHome),
            WKC::BrowserRefresh => Ok(Self::BrowserRefresh),
            WKC::BrowserSearch => Ok(Self::BrowserSearch),
            WKC::BrowserStop => Ok(Self::BrowserStop),
            WKC::Eject => Ok(Self::Eject),
            WKC::LaunchApp1 => Ok(Self::LaunchApp1),
            WKC::LaunchApp2 => Ok(Self::LaunchApp2),
            WKC::LaunchMail => Ok(Self::LaunchMail),
            WKC::MediaPlayPause => Ok(Self::MediaPlayPause),
            WKC::MediaSelect => Ok(Self::MediaSelect),
            WKC::MediaStop => Ok(Self::MediaStop),
            WKC::MediaTrackNext => Ok(Self::MediaTrackNext),
            WKC::MediaTrackPrevious => Ok(Self::MediaTrackPrevious),
            WKC::Power => Ok(Self::Power),
            WKC::Sleep => Ok(Self::Sleep),
            WKC::AudioVolumeDown => Ok(Self::AudioVolumeDown),
            WKC::AudioVolumeMute => Ok(Self::AudioVolumeMute),
            WKC::AudioVolumeUp => Ok(Self::AudioVolumeUp),
            WKC::WakeUp => Ok(Self::WakeUp),
            WKC::Meta => Ok(Self::Meta),
            WKC::Hyper => Ok(Self::Hyper),
            WKC::Turbo => Ok(Self::Turbo),
            WKC::Abort => Ok(Self::Abort),
            WKC::Resume => Ok(Self::Resume),
            WKC::Suspend => Ok(Self::Suspend),
            WKC::Again => Ok(Self::Again),
            WKC::Copy => Ok(Self::Copy),
            WKC::Cut => Ok(Self::Cut),
            WKC::Find => Ok(Self::Find),
            WKC::Open => Ok(Self::Open),
            WKC::Paste => Ok(Self::Paste),
            WKC::Props => Ok(Self::Props),
            WKC::Select => Ok(Self::Select),
            WKC::Undo => Ok(Self::Undo),
            WKC::Hiragana => Ok(Self::Hiragana),
            WKC::Katakana => Ok(Self::Katakana),
            WKC::F1 => Ok(Self::F1),
            WKC::F2 => Ok(Self::F2),
            WKC::F3 => Ok(Self::F3),
            WKC::F4 => Ok(Self::F4),
            WKC::F5 => Ok(Self::F5),
            WKC::F6 => Ok(Self::F6),
            WKC::F7 => Ok(Self::F7),
            WKC::F8 => Ok(Self::F8),
            WKC::F9 => Ok(Self::F9),
            WKC::F10 => Ok(Self::F10),
            WKC::F11 => Ok(Self::F11),
            WKC::F12 => Ok(Self::F12),
            WKC::F13 => Ok(Self::F13),
            WKC::F14 => Ok(Self::F14),
            WKC::F15 => Ok(Self::F15),
            WKC::F16 => Ok(Self::F16),
            WKC::F17 => Ok(Self::F17),
            WKC::F18 => Ok(Self::F18),
            WKC::F19 => Ok(Self::F19),
            WKC::F20 => Ok(Self::F20),
            WKC::F21 => Ok(Self::F21),
            WKC::F22 => Ok(Self::F22),
            WKC::F23 => Ok(Self::F23),
            WKC::F24 => Ok(Self::F24),
            WKC::F25 => Ok(Self::F25),
            WKC::F26 => Ok(Self::F26),
            WKC::F27 => Ok(Self::F27),
            WKC::F28 => Ok(Self::F28),
            WKC::F29 => Ok(Self::F29),
            WKC::F30 => Ok(Self::F30),
            WKC::F31 => Ok(Self::F31),
            WKC::F32 => Ok(Self::F32),
            WKC::F33 => Ok(Self::F33),
            WKC::F34 => Ok(Self::F34),
            WKC::F35 => Ok(Self::F35),
            _ => Err(*value),
        }
    }
}

// If these assertions fail, the constant values in the C# code must also be changed.
static_assertions::const_assert_eq!(wgpu::COPY_BYTES_PER_ROW_ALIGNMENT, 256);
static_assertions::const_assert_eq!(wgpu::QUERY_RESOLVE_BUFFER_ALIGNMENT, 256);
static_assertions::const_assert_eq!(wgpu::COPY_BUFFER_ALIGNMENT, 4);
static_assertions::const_assert_eq!(wgpu::MAP_ALIGNMENT, 8);
static_assertions::const_assert_eq!(wgpu::VERTEX_STRIDE_ALIGNMENT, 4);
static_assertions::const_assert_eq!(wgpu::PUSH_CONSTANT_ALIGNMENT, 4);
static_assertions::const_assert_eq!(wgpu::QUERY_SET_MAX_QUERIES, 4096);
static_assertions::const_assert_eq!(wgpu::QUERY_SIZE, 8);

pub(crate) type ScreenInitFn =
    extern "cdecl" fn(screen: Box<Screen>, screen_info: &ScreenInfo) -> ScreenId;
pub(crate) type EngineUnhandledErrorFn = extern "cdecl" fn(message: *const u8, len: usize);
pub(crate) type ClearedEventFn = extern "cdecl" fn(screen_id: ScreenId);
pub(crate) type RedrawRequestedEventFn = extern "cdecl" fn(screen_id: ScreenId) -> bool;
pub(crate) type ResizedEventFn = extern "cdecl" fn(screen_id: ScreenId, width: u32, height: u32);
pub(crate) type KeyboardEventFn =
    extern "cdecl" fn(screen_id: ScreenId, key: KeyCode, pressed: bool);

pub(crate) type CharReceivedEventFn = extern "cdecl" fn(screen_id: ScreenId, input: u32);
pub(crate) type MouseButtonEventFn =
    extern "cdecl" fn(screen_id: ScreenId, button: MouseButton, pressed: bool);
pub(crate) type ImeInputEventFn = extern "cdecl" fn(screen_id: ScreenId, input: &ImeInputData);
pub(crate) type MouseWheelEventFn =
    extern "cdecl" fn(screen_id: ScreenId, x_delta: f32, y_delta: f32);
pub(crate) type CursorMovedEventFn = extern "cdecl" fn(screen_id: ScreenId, x: f32, y: f32);
pub(crate) type CursorEnteredLeftEventFn = extern "cdecl" fn(screen_id: ScreenId, entered: bool);
pub(crate) type ClosingEventFn = extern "cdecl" fn(screen_id: ScreenId, cancel: &mut bool);
pub(crate) type ClosedEventFn = extern "cdecl" fn(screen_id: ScreenId) -> Option<Box<Screen>>;
pub(crate) type DebugPrintlnFn = extern "cdecl" fn(message: *const u8, len: usize);
