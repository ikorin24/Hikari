use crate::engine::HostScreen;
use bytemuck::Contiguous;
use smallvec::SmallVec;
use static_assertions::assert_eq_size;
use std;
use std::ffi;
use std::{marker, num, ops, str};

#[repr(C)]
pub(crate) struct EngineCoreConfig {
    pub on_screen_init: HostScreenInitFn,
}

#[repr(C)]
#[derive(Debug)]
pub(crate) struct HostScreenConfig<'a> {
    pub title: Slice<'a, u8>,
    pub style: WindowStyle,
    pub width: u32,
    pub height: u32,
    pub backend: wgpu::Backends,
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
#[derive(Default)]
pub(crate) struct HostScreenCallbacks {
    pub on_render: Option<HostScreenRenderFn>,
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
    pub const fn to_wgpu_type(&self) -> wgpu::ImageCopyTexture {
        wgpu::ImageCopyTexture {
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
pub(crate) struct TextureViewDescriptor {
    pub format: Opt<TextureFormat>,
    pub dimension: Opt<TextureViewDimension>,
    pub aspect: TextureAspect,
    pub base_mip_level: u32,
    pub mip_level_count: u32,
    pub base_array_layer: u32,
    pub array_layer_count: u32,
}

impl TextureViewDescriptor {
    pub fn to_wgpu_type(&self) -> wgpu::TextureViewDescriptor {
        wgpu::TextureViewDescriptor {
            label: None,
            format: self.format.map_to_option(|x| x.to_wgpu_type()),
            dimension: self.dimension.map_to_option(|x| x.to_wgpu_type()),
            aspect: self.aspect.to_wgpu_type(),
            base_mip_level: self.base_mip_level,
            mip_level_count: num::NonZeroU32::from_integer(self.mip_level_count),
            base_array_layer: self.base_array_layer,
            array_layer_count: num::NonZeroU32::from_integer(self.array_layer_count),
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
    pub anisotropy_clamp: u8,
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
            anisotropy_clamp: num::NonZeroU8::from_integer(self.anisotropy_clamp),
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
    pub fn to_wgpu_type(&self) -> wgpu::SamplerBorderColor {
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
        let mut wgpu_group_entries = vec![];
        let wgpu_buffer_bindings_vec = self
            .entries
            .iter()
            .map(|entry| match entry.resource {
                BindingResource {
                    tag: BindingResourceTag::BufferArray,
                    ref payload,
                } => payload
                    .as_ref_unwrap::<Slice<BufferBinding>>()
                    .iter()
                    .map(|bb| bb.to_wgpu_type())
                    .collect::<Vec<_>>(),
                _ => vec![],
            })
            .collect::<Vec<_>>();

        for (entry_index, entry) in self.entries.iter().enumerate() {
            let wgpu_binding_resource = match entry.resource {
                BindingResource {
                    tag: BindingResourceTag::Buffer,
                    ref payload,
                } => wgpu::BindingResource::Buffer(
                    payload.as_ref_unwrap::<BufferBinding>().to_wgpu_type(),
                ),
                BindingResource {
                    tag: BindingResourceTag::BufferArray,
                    ..
                } => wgpu::BindingResource::BufferArray(&wgpu_buffer_bindings_vec[entry_index]),
                BindingResource {
                    tag: BindingResourceTag::Sampler,
                    ref payload,
                } => wgpu::BindingResource::Sampler(payload.as_ref_unwrap::<wgpu::Sampler>()),
                BindingResource {
                    tag: BindingResourceTag::SamplerArray,
                    ref payload,
                } => wgpu::BindingResource::SamplerArray(
                    payload.as_ref_unwrap::<Slice<&wgpu::Sampler>>(),
                ),
                BindingResource {
                    tag: BindingResourceTag::TextureView,
                    ref payload,
                } => {
                    wgpu::BindingResource::TextureView(payload.as_ref_unwrap::<wgpu::TextureView>())
                }
                BindingResource {
                    tag: BindingResourceTag::TextureViewArray,
                    ref payload,
                } => wgpu::BindingResource::TextureViewArray(
                    payload.as_ref_unwrap::<Slice<&wgpu::TextureView>>(),
                ),
            };
            wgpu_group_entries.push(wgpu::BindGroupEntry {
                binding: entry.binding,
                resource: wgpu_binding_resource,
            });
        }
        let wgpu_bind_group_desc = wgpu::BindGroupDescriptor {
            label: None,
            layout: self.layout,
            entries: &wgpu_group_entries,
        };
        consume(&wgpu_bind_group_desc)
    }
}

#[repr(C)]
pub(crate) struct BindGroupEntry<'a> {
    pub binding: u32,
    pub resource: BindingResource<'a>,
}

#[repr(C)]
pub(crate) struct BindingResource<'a> {
    pub tag: BindingResourceTag,
    /// `&BufferBinding` or `&Slice<BufferBinding>`
    /// or `&wgpu::Sampler` or `&Slice<&wgpu::Sampler>`
    /// or `&wgpu::TextureView` or `&Slice<&wgpu::TextureView>`
    pub payload: PointerWrap<'a>,
}

#[repr(transparent)]
pub(crate) struct PointerWrap<'a> {
    ptr: *const ffi::c_void,
    phantom: marker::PhantomData<&'a ffi::c_void>,
}

impl<'a> PointerWrap<'a> {
    pub fn as_ref_unwrap<T>(&self) -> &'a T {
        unsafe { (self.ptr as *const T).as_ref() }.unwrap()
    }
}

#[repr(u32)]
#[derive(Debug, PartialEq, Eq, Clone, Copy)]
#[allow(dead_code)] // because values are from FFI
pub(crate) enum BindingResourceTag {
    Buffer = 0,
    BufferArray = 1,
    Sampler = 2,
    SamplerArray = 3,
    TextureView = 4,
    TextureViewArray = 5,
}

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
            size: num::NonZeroU64::from_integer(self.size),
        }
    }
}

#[repr(C)]
pub(crate) struct PipelineLayoutDescriptor<'a> {
    pub bind_group_layouts: Slice<'a, &'a wgpu::BindGroupLayout>,
}

impl<'a> PipelineLayoutDescriptor<'a> {
    pub fn to_pipeline_descriptor(&self) -> wgpu::PipelineLayoutDescriptor {
        wgpu::PipelineLayoutDescriptor {
            label: None,
            bind_group_layouts: &self.bind_group_layouts,
            push_constant_ranges: &[],
        }
    }
}

#[repr(C)]
pub(crate) struct RenderPipelineDescription<'a> {
    pub layout: &'a wgpu::PipelineLayout,
    pub vertex: VertexState<'a>,
    pub fragment: Opt<FragmentState<'a>>,
    pub primitive: PrimitiveState,
}

impl<'a> RenderPipelineDescription<'a> {
    pub fn use_wgpu_type<T>(
        &self,
        consume: impl FnOnce(&wgpu::RenderPipelineDescriptor) -> T,
    ) -> T {
        let vertex = wgpu::VertexState {
            module: self.vertex.module,
            entry_point: self.vertex.entry_point.as_str().unwrap(),
            buffers: &self
                .vertex
                .inputs
                .iter()
                .map(|x| x.to_wgpu_type())
                .collect::<SmallVec<[_; 4]>>(),
        };

        let mut fragment_targets = vec![];
        let fragment = match self.fragment {
            Opt {
                exists: true,
                ref value,
            } => {
                fragment_targets.extend(
                    value
                        .targets
                        .iter()
                        .map(|x| x.map_to_option(|y| y.to_wgpu_type())),
                );
                Some(wgpu::FragmentState {
                    module: value.module,
                    entry_point: value.entry_point.as_str().unwrap(),
                    targets: &fragment_targets,
                })
            }
            _ => None,
        };

        let pipeline_desc = wgpu::RenderPipelineDescriptor {
            label: None,
            layout: Some(self.layout),
            vertex,
            fragment,
            primitive: self.primitive.to_wgpu_type(),
            depth_stencil: None,
            multisample: wgpu::MultisampleState {
                count: 1,
                mask: !0,
                alpha_to_coverage_enabled: false,
            },
            multiview: None,
        };
        consume(&pipeline_desc)
    }
}

#[repr(C)]
pub(crate) struct VertexState<'a> {
    pub module: &'a wgpu::ShaderModule,
    pub entry_point: Slice<'a, u8>,
    pub inputs: Slice<'a, VertexBufferLayout<'a>>,
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
            count: num::NonZeroU32::from_integer(self.count),
        }
    }
}

#[repr(C)]
pub(crate) struct BindingType<'a> {
    pub tag: BindingTypeTag,
    /// `BufferBindingData` or `SamplerBindingType`
    /// or `TextureBindingData` or `StorageTextureBindingData`
    pub payload: PointerWrap<'a>,
}

impl<'a> BindingType<'a> {
    pub fn to_wgpu_type(&self) -> wgpu::BindingType {
        match self.tag {
            BindingTypeTag::Buffer => {
                let payload = self.payload.as_ref_unwrap::<BufferBindingData>();
                wgpu::BindingType::Buffer {
                    ty: payload.ty.to_wgpu_type(),
                    has_dynamic_offset: payload.has_dynamic_offset,
                    min_binding_size: num::NonZeroU64::from_integer(payload.min_binding_size),
                }
            }
            BindingTypeTag::Sampler => wgpu::BindingType::Sampler(
                self.payload
                    .as_ref_unwrap::<SamplerBindingType>()
                    .to_wgpu_type(),
            ),
            BindingTypeTag::Texture => {
                let payload = self.payload.as_ref_unwrap::<TextureBindingData>();
                wgpu::BindingType::Texture {
                    sample_type: payload.sample_type.to_wgpu_type(),
                    view_dimension: payload.view_dimension.to_wgpu_type(),
                    multisampled: payload.multisampled,
                }
            }
            BindingTypeTag::StorageTexture => {
                let payload = self.payload.as_ref_unwrap::<StorageTextureBindingData>();
                wgpu::BindingType::StorageTexture {
                    access: payload.access.to_wgpu_type(),
                    format: payload.format.to_wgpu_type(),
                    view_dimension: payload.view_dimension.to_wgpu_type(),
                }
            }
        }
    }
}

#[repr(u32)]
#[derive(Debug, PartialEq, Eq, Clone, Copy)]
#[allow(dead_code)] // because values are from FFI
pub(crate) enum BindingTypeTag {
    Buffer = 0,
    Sampler = 1,
    Texture = 2,
    StorageTexture = 3,
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
#[derive(Debug, PartialEq, Eq, Clone, Copy)]
#[allow(dead_code)] // because values are from FFI
pub(crate) enum TextureSampleType {
    FloatFilterable = 0,
    FloatNotFilterable = 1,
    Depth = 2,
    Sint = 3,
    Uint = 4,
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
    Rg11b10Float = 29,

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
    Depth24UnormStencil8 = 45,

    // Packed uncompressed texture formats
    /// Packed unsigned float with 9 bits mantisa for each RGB component, then a common 5 bits exponent
    Rgb9e5Ufloat = 46,

    // Compressed textures usable with `TEXTURE_COMPRESSION_BC` feature.
    /// 4x4 block compressed texture. 8 bytes per block (4 bit/px). 4 color + alpha pallet. 5 bit R + 6 bit G + 5 bit B + 1 bit alpha.
    /// [0, 63] ([0, 1] for alpha) converted to/from float [0, 1] in shader.
    ///
    /// Also known as DXT1.
    ///
    /// [`Features::TEXTURE_COMPRESSION_BC`] must be enabled to use this texture format.
    Bc1RgbaUnorm = 47,
    /// 4x4 block compressed texture. 8 bytes per block (4 bit/px). 4 color + alpha pallet. 5 bit R + 6 bit G + 5 bit B + 1 bit alpha.
    /// Srgb-color [0, 63] ([0, 1] for alpha) converted to/from linear-color float [0, 1] in shader.
    ///
    /// Also known as DXT1.
    ///
    /// [`Features::TEXTURE_COMPRESSION_BC`] must be enabled to use this texture format.
    Bc1RgbaUnormSrgb = 48,
    /// 4x4 block compressed texture. 16 bytes per block (8 bit/px). 4 color pallet. 5 bit R + 6 bit G + 5 bit B + 4 bit alpha.
    /// [0, 63] ([0, 15] for alpha) converted to/from float [0, 1] in shader.
    ///
    /// Also known as DXT3.
    ///
    /// [`Features::TEXTURE_COMPRESSION_BC`] must be enabled to use this texture format.
    Bc2RgbaUnorm = 49,
    /// 4x4 block compressed texture. 16 bytes per block (8 bit/px). 4 color pallet. 5 bit R + 6 bit G + 5 bit B + 4 bit alpha.
    /// Srgb-color [0, 63] ([0, 255] for alpha) converted to/from linear-color float [0, 1] in shader.
    ///
    /// Also known as DXT3.
    ///
    /// [`Features::TEXTURE_COMPRESSION_BC`] must be enabled to use this texture format.
    Bc2RgbaUnormSrgb = 50,
    /// 4x4 block compressed texture. 16 bytes per block (8 bit/px). 4 color pallet + 8 alpha pallet. 5 bit R + 6 bit G + 5 bit B + 8 bit alpha.
    /// [0, 63] ([0, 255] for alpha) converted to/from float [0, 1] in shader.
    ///
    /// Also known as DXT5.
    ///
    /// [`Features::TEXTURE_COMPRESSION_BC`] must be enabled to use this texture format.
    Bc3RgbaUnorm = 51,
    /// 4x4 block compressed texture. 16 bytes per block (8 bit/px). 4 color pallet + 8 alpha pallet. 5 bit R + 6 bit G + 5 bit B + 8 bit alpha.
    /// Srgb-color [0, 63] ([0, 255] for alpha) converted to/from linear-color float [0, 1] in shader.
    ///
    /// Also known as DXT5.
    ///
    /// [`Features::TEXTURE_COMPRESSION_BC`] must be enabled to use this texture format.
    Bc3RgbaUnormSrgb = 52,
    /// 4x4 block compressed texture. 8 bytes per block (4 bit/px). 8 color pallet. 8 bit R.
    /// [0, 255] converted to/from float [0, 1] in shader.
    ///
    /// Also known as RGTC1.
    ///
    /// [`Features::TEXTURE_COMPRESSION_BC`] must be enabled to use this texture format.
    Bc4RUnorm = 53,
    /// 4x4 block compressed texture. 8 bytes per block (4 bit/px). 8 color pallet. 8 bit R.
    /// [-127, 127] converted to/from float [-1, 1] in shader.
    ///
    /// Also known as RGTC1.
    ///
    /// [`Features::TEXTURE_COMPRESSION_BC`] must be enabled to use this texture format.
    Bc4RSnorm = 54,
    /// 4x4 block compressed texture. 16 bytes per block (8 bit/px). 8 color red pallet + 8 color green pallet. 8 bit RG.
    /// [0, 255] converted to/from float [0, 1] in shader.
    ///
    /// Also known as RGTC2.
    ///
    /// [`Features::TEXTURE_COMPRESSION_BC`] must be enabled to use this texture format.
    Bc5RgUnorm = 55,
    /// 4x4 block compressed texture. 16 bytes per block (8 bit/px). 8 color red pallet + 8 color green pallet. 8 bit RG.
    /// [-127, 127] converted to/from float [-1, 1] in shader.
    ///
    /// Also known as RGTC2.
    ///
    /// [`Features::TEXTURE_COMPRESSION_BC`] must be enabled to use this texture format.
    Bc5RgSnorm = 56,
    /// 4x4 block compressed texture. 16 bytes per block (8 bit/px). Variable sized pallet. 16 bit unsigned float RGB. Float in shader.
    ///
    /// Also known as BPTC (float).
    ///
    /// [`Features::TEXTURE_COMPRESSION_BC`] must be enabled to use this texture format.
    Bc6hRgbUfloat = 57,
    /// 4x4 block compressed texture. 16 bytes per block (8 bit/px). Variable sized pallet. 16 bit signed float RGB. Float in shader.
    ///
    /// Also known as BPTC (float).
    ///
    /// [`Features::TEXTURE_COMPRESSION_BC`] must be enabled to use this texture format.
    Bc6hRgbSfloat = 58,
    /// 4x4 block compressed texture. 16 bytes per block (8 bit/px). Variable sized pallet. 8 bit integer RGBA.
    /// [0, 255] converted to/from float [0, 1] in shader.
    ///
    /// Also known as BPTC (unorm).
    ///
    /// [`Features::TEXTURE_COMPRESSION_BC`] must be enabled to use this texture format.
    Bc7RgbaUnorm = 59,
    /// 4x4 block compressed texture. 16 bytes per block (8 bit/px). Variable sized pallet. 8 bit integer RGBA.
    /// Srgb-color [0, 255] converted to/from linear-color float [0, 1] in shader.
    ///
    /// Also known as BPTC (unorm).
    ///
    /// [`Features::TEXTURE_COMPRESSION_BC`] must be enabled to use this texture format.
    Bc7RgbaUnormSrgb = 60,
    /// 4x4 block compressed texture. 8 bytes per block (4 bit/px). Complex pallet. 8 bit integer RGB.
    /// [0, 255] converted to/from float [0, 1] in shader.
    ///
    /// [`Features::TEXTURE_COMPRESSION_ETC2`] must be enabled to use this texture format.
    Etc2Rgb8Unorm = 61,
    /// 4x4 block compressed texture. 8 bytes per block (4 bit/px). Complex pallet. 8 bit integer RGB.
    /// Srgb-color [0, 255] converted to/from linear-color float [0, 1] in shader.
    ///
    /// [`Features::TEXTURE_COMPRESSION_ETC2`] must be enabled to use this texture format.
    Etc2Rgb8UnormSrgb = 62,
    /// 4x4 block compressed texture. 8 bytes per block (4 bit/px). Complex pallet. 8 bit integer RGB + 1 bit alpha.
    /// [0, 255] ([0, 1] for alpha) converted to/from float [0, 1] in shader.
    ///
    /// [`Features::TEXTURE_COMPRESSION_ETC2`] must be enabled to use this texture format.
    Etc2Rgb8A1Unorm = 63,
    /// 4x4 block compressed texture. 8 bytes per block (4 bit/px). Complex pallet. 8 bit integer RGB + 1 bit alpha.
    /// Srgb-color [0, 255] ([0, 1] for alpha) converted to/from linear-color float [0, 1] in shader.
    ///
    /// [`Features::TEXTURE_COMPRESSION_ETC2`] must be enabled to use this texture format.
    Etc2Rgb8A1UnormSrgb = 64,
    /// 4x4 block compressed texture. 16 bytes per block (8 bit/px). Complex pallet. 8 bit integer RGB + 8 bit alpha.
    /// [0, 255] converted to/from float [0, 1] in shader.
    ///
    /// [`Features::TEXTURE_COMPRESSION_ETC2`] must be enabled to use this texture format.
    Etc2Rgba8Unorm = 65,
    /// 4x4 block compressed texture. 16 bytes per block (8 bit/px). Complex pallet. 8 bit integer RGB + 8 bit alpha.
    /// Srgb-color [0, 255] converted to/from linear-color float [0, 1] in shader.
    ///
    /// [`Features::TEXTURE_COMPRESSION_ETC2`] must be enabled to use this texture format.
    Etc2Rgba8UnormSrgb = 66,
    /// 4x4 block compressed texture. 8 bytes per block (4 bit/px). Complex pallet. 11 bit integer R.
    /// [0, 255] converted to/from float [0, 1] in shader.
    ///
    /// [`Features::TEXTURE_COMPRESSION_ETC2`] must be enabled to use this texture format.
    EacR11Unorm = 67,
    /// 4x4 block compressed texture. 8 bytes per block (4 bit/px). Complex pallet. 11 bit integer R.
    /// [-127, 127] converted to/from float [-1, 1] in shader.
    ///
    /// [`Features::TEXTURE_COMPRESSION_ETC2`] must be enabled to use this texture format.
    EacR11Snorm = 68,
    /// 4x4 block compressed texture. 16 bytes per block (8 bit/px). Complex pallet. 11 bit integer R + 11 bit integer G.
    /// [0, 255] converted to/from float [0, 1] in shader.
    ///
    /// [`Features::TEXTURE_COMPRESSION_ETC2`] must be enabled to use this texture format.
    EacRg11Unorm = 69,
    /// 4x4 block compressed texture. 16 bytes per block (8 bit/px). Complex pallet. 11 bit integer R + 11 bit integer G.
    /// [-127, 127] converted to/from float [-1, 1] in shader.
    ///
    /// [`Features::TEXTURE_COMPRESSION_ETC2`] must be enabled to use this texture format.
    EacRg11Snorm = 70,
}

impl TextureFormat {
    pub fn to_wgpu_type(&self) -> wgpu::TextureFormat {
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
            Self::Rg11b10Float => wgpu::TextureFormat::Rg11b10Float,
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
            Self::Depth24UnormStencil8 => wgpu::TextureFormat::Depth24UnormStencil8,
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
            Self::Bc6hRgbSfloat => wgpu::TextureFormat::Bc6hRgbSfloat,
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
            wgpu::TextureFormat::Rg11b10Float => Ok(Self::Rg11b10Float),
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
            wgpu::TextureFormat::Depth24UnormStencil8 => Ok(Self::Depth24UnormStencil8),
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
            wgpu::TextureFormat::Bc6hRgbSfloat => Ok(Self::Bc6hRgbSfloat),
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

impl TextureDescriptor {
    pub fn to_wgpu_type(&self) -> wgpu::TextureDescriptor<'static> {
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

impl TextureDimension {
    pub fn to_wgpu_type(&self) -> wgpu::TextureDimension {
        match self {
            Self::D1 => wgpu::TextureDimension::D1,
            Self::D2 => wgpu::TextureDimension::D2,
            Self::D3 => wgpu::TextureDimension::D3,
        }
    }
}

#[repr(C)]
pub(crate) struct VertexBufferLayout<'a> {
    pub vertex_size: u64,
    pub attributes: Slice<'a, wgpu::VertexAttribute>,
}

impl<'a> VertexBufferLayout<'a> {
    pub fn to_wgpu_type(&self) -> wgpu::VertexBufferLayout {
        wgpu::VertexBufferLayout {
            array_stride: self.vertex_size,
            step_mode: wgpu::VertexStepMode::Vertex,
            attributes: &self.attributes,
        }
    }
}

/// ffi-safe `Option<T>`
/// (use `Option<T>` if T is reference)
#[repr(C)]
pub(crate) struct Opt<T> {
    pub exists: bool,
    pub value: T,
}

impl<T> Opt<T> {
    pub fn map_to_option<U, F>(&self, f: F) -> Option<U>
    where
        F: FnOnce(&T) -> U,
    {
        match self.exists {
            true => Some(f(&self.value)),
            false => None,
        }
    }
}

impl<T: Default> From<Option<T>> for Opt<T> {
    fn from(o: Option<T>) -> Self {
        match o {
            Some(x) => Opt {
                exists: true,
                value: x,
            },
            None => Opt {
                exists: false,
                value: T::default(),
            },
        }
    }
}

impl<T: Copy> Opt<T> {
    pub fn to_option(&self) -> Option<T> {
        match self.exists {
            true => Some(self.value),
            false => None,
        }
    }
}

#[repr(C)]
#[derive(Debug)]
pub(crate) struct BufSlice<'a> {
    pub buffer: &'a wgpu::Buffer,
    pub range: RangeBoundsU64,
}

impl<'a> BufSlice<'a> {
    pub fn to_buffer_slice(&self) -> wgpu::BufferSlice<'a> {
        self.buffer.slice(self.range)
    }
}

#[repr(C)]
#[derive(Clone, Copy, Debug)]
pub(crate) struct Slice<'a, T> {
    pub data: Option<&'a T>,
    pub len: usize,
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
        match self.data {
            Some(data) => unsafe { std::slice::from_raw_parts(data as *const T, self.len) },
            None => &[],
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
#[derive(Debug)]
pub(crate) struct DrawBufferArg<'a> {
    pub vertex_buffer: SlotBufSlice<'a>,
    pub vertices_range: RangeU32,
    pub instances_range: RangeU32,
}

#[repr(C)]
#[derive(Debug)]
pub(crate) struct DrawBufferIndexedArg<'a> {
    pub vertex_buffer_slice: BufSlice<'a>,
    pub slot: u32,
    pub index_buffer_slice: BufSlice<'a>,
    pub index_format: wgpu::IndexFormat,
    pub index_start: u32,
    pub index_end_excluded: u32,
    pub instance_start: u32,
    pub instance_end_excluded: u32,
}

#[repr(C)]
#[derive(Debug)]
pub(crate) struct DrawBuffersIndexedArg<'a> {
    pub vertex_buffers: Slice<'a, SlotBufSlice<'a>>,
    pub index_buffer_slice: BufSlice<'a>,
    pub index_format: wgpu::IndexFormat,
    pub index_start: u32,
    pub index_end_excluded: u32,
    pub instance_start: u32,
    pub instance_end_excluded: u32,
}

#[repr(C)]
#[derive(Debug)]
pub(crate) struct SlotBufSlice<'a> {
    pub buffer_slice: BufSlice<'a>,
    pub slot: u32,
}

#[repr(C)]
#[derive(Debug)]
pub(crate) struct IndexBufSlice<'a> {
    pub buffer_slice: BufSlice<'a>,
    pub format: wgpu::IndexFormat,
}

#[repr(C)]
#[derive(Clone, Copy, Debug)]
pub(crate) struct RangeU64 {
    pub start: u64,
    pub end_excluded: u64,
}

impl RangeU64 {
    // pub fn to_range(&self) -> ops::Range<u64> {
    //     ops::Range {
    //         start: self.start,
    //         end: self.end_excluded,
    //     }
    // }
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
#[derive(Clone, Copy, Debug)]
pub(crate) struct RangeBoundsU32 {
    pub start: u32,
    pub end_excluded: u32,
    pub has_start: bool,
    pub has_end_excluded: bool,
}

impl ops::RangeBounds<u32> for RangeBoundsU32 {
    fn start_bound(&self) -> ops::Bound<&u32> {
        if self.has_start {
            ops::Bound::Included(&self.start)
        } else {
            ops::Bound::Unbounded
        }
    }

    fn end_bound(&self) -> ops::Bound<&u32> {
        if self.has_end_excluded {
            ops::Bound::Excluded(&self.end_excluded)
        } else {
            ops::Bound::Unbounded
        }
    }
}

#[repr(C)]
pub(crate) struct HostScreenInfo {
    pub backend: wgpu::Backend,
    pub surface_format: Opt<TextureFormat>,
}

pub(crate) type HostScreenInitFn =
    extern "cdecl" fn(screen: &mut HostScreen, screen_info: &HostScreenInfo) -> HostScreenCallbacks;
pub(crate) type HostScreenRenderFn =
    extern "cdecl" fn(screen: &mut HostScreen, render_pass: &mut wgpu::RenderPass) -> ();

// -------------------------

mod compile_time_test {

    /// assert that `repr(C)` enum has 32bits size in current platform target.
    #[repr(u32)]
    #[allow(dead_code)] // because the enum is only for compile time
    enum OnlyForCompileTimeSizeCheck {
        A = 0,
    }
    static_assertions::assert_eq_size!(OnlyForCompileTimeSizeCheck, u32);
}
