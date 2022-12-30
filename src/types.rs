use crate::engine::HostScreen;
use std;
use std::{error, fmt, ops, str};

pub(crate) enum WindowStyle {
    Default,
    Fixed,
    Fullscreen,
}

impl str::FromStr for WindowStyle {
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
pub(crate) struct ParseEnumError {
    pub string: String,
}

impl fmt::Display for ParseEnumError {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(f, "cannot parse str: {}", self.string)
    }
}

impl error::Error for ParseEnumError {}

#[repr(C)]
#[derive(Default)]
pub(crate) struct HostScreenCallbacks {
    pub on_render: Option<HostScreenRenderFn>,
}

#[repr(C)]
pub(crate) struct RenderPipelineInfo {
    pub vertex: VertexLayoutInfo,
    pub shader_source: Sliceffi<u8>,
}

#[repr(C)]
pub(crate) struct VertexLayoutInfo {
    pub vertex_size: u64,
    pub attributes: Sliceffi<wgpu::VertexAttribute>,
}

impl VertexLayoutInfo {
    pub fn to_vertex_buffer_layout(&self) -> wgpu::VertexBufferLayout {
        wgpu::VertexBufferLayout {
            array_stride: self.vertex_size,
            step_mode: wgpu::VertexStepMode::Vertex,
            attributes: self.attributes.as_slice(),
        }
    }
}

#[repr(C)]
pub(crate) struct BufferSliceffi<'a> {
    buffer: &'a wgpu::Buffer,
    range: RangeBoundsU64ffi,
}

impl<'a> BufferSliceffi<'a> {
    pub fn to_buffer_slice(&self) -> wgpu::BufferSlice {
        self.buffer.slice(self.range)
    }
}

#[repr(C)]
#[derive(Clone, Copy, Debug)]
pub(crate) struct Sliceffi<T> {
    data: *const T,
    len: usize,
}

impl<T> Sliceffi<T> {
    pub fn as_slice(&self) -> &[T] {
        unsafe { std::slice::from_raw_parts(self.data, self.len) }
    }
}

impl Sliceffi<u8> {
    pub fn as_str(&self) -> Result<&str, str::Utf8Error> {
        std::str::from_utf8(self.as_slice())
    }
}

#[repr(C)]
#[derive(Clone, Copy, Debug)]
pub(crate) struct RangeU64ffi {
    start: u64,
    end_excluded: u64,
}

impl RangeU64ffi {
    pub fn to_range(&self) -> ops::Range<u64> {
        ops::Range {
            start: self.start,
            end: self.end_excluded,
        }
    }
}

#[repr(C)]
#[derive(Clone, Copy, Debug)]
pub(crate) struct RangeU32ffi {
    start: u32,
    end_excluded: u32,
}

impl RangeU32ffi {
    pub fn to_range(&self) -> ops::Range<u32> {
        ops::Range {
            start: self.start,
            end: self.end_excluded,
        }
    }
}

#[repr(C)]
#[derive(Clone, Copy, Debug)]
pub(crate) struct RangeBoundsU64ffi {
    start: u64,
    end_excluded: u64,
    has_start: bool,
    has_end_excluded: bool,
}

impl ops::RangeBounds<u64> for RangeBoundsU64ffi {
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
pub(crate) struct RangeBoundsU32ffi {
    start: u32,
    end_excluded: u32,
    has_start: bool,
    has_end_excluded: bool,
}

impl ops::RangeBounds<u32> for RangeBoundsU32ffi {
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

pub(crate) type HostScreenInitFn =
    extern "cdecl" fn(screen: &mut HostScreen) -> HostScreenCallbacks;
pub(crate) type HostScreenRenderFn =
    extern "cdecl" fn(screen: &mut HostScreen, render_pass: &mut wgpu::RenderPass) -> ();
