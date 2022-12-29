use renderer::HostScreen;

mod host_screen;
mod renderer;

// type ErrorCallback = extern "cdecl" fn(*const u8, usize);

// #[no_mangle]
// pub extern "cdecl" fn start__(on_err: Option<ErrorCallback>) {
//     let on_err = on_err.unwrap();
//     match host_screen::start() {
//         Ok(_) => {}
//         Err(err) => {
//             let err_string = err.to_string();
//             on_err(err_string.as_ptr(), err_string.len());
//         }
//     }
// }

type HostScreenInitFn = extern "cdecl" fn(&mut HostScreen) -> ();

#[no_mangle]
extern "cdecl" fn elffy_engine_start(init: HostScreenInitFn) {
    host_screen::start_engine_start(init);
    // let on_err = on_err.unwrap();
    // match host_screen::start() {
    //     Ok(_) => {}
    //     Err(err) => {
    //         let err_string = err.to_string();
    //         on_err(err_string.as_ptr(), err_string.len());
    //     }
    // }
}

// assert_eq_size!(winit::window::WindowId, usize);
// assert_eq_size!(Option<&VertexLayoutInfo>, usize);

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

// #[no_mangle]
// pub extern "cdecl" fn start(on_err: Option<ErrorCallback>) {
//     let on_err = on_err.unwrap();
//     match host_screen::start() {
//         Ok(_) => {}
//         Err(err) => {
//             let err_string = err.to_string();
//             on_err(err_string.as_ptr(), err_string.len());
//         }
//     }
// }

pub type HostScreenId = usize;

// #[repr(C)]
// #[derive(Clone, Copy, Debug)]
// pub struct Handle<'a, T>(&'a T);

// impl<T> AsRef<T> for Handle<'_, T> {
//     fn as_ref(&self) -> &T {
//         self.0
//     }
// }

// impl<T> From<&T> for Handle<'_, T> {
//     fn from(a: &T) -> Self {
//         Self(a)
//     }
// }

// #[repr(C)]
// pub struct Utf8Str<'a> {
//     data: &'a u8,
//     len: usize,
// }

// impl Utf8Str<'_> {
//     pub fn as_str(&self) -> Result<&str, Utf8Error> {
//         let slice = unsafe { slice::from_raw_parts(self.data as *const u8, self.len) };
//         std::str::from_utf8(slice)
//     }
// }

#[repr(C)]
pub struct RenderPipelineInfo<'a> {
    pub vertex: VertexLayoutInfo<'a>,
    pub shader_source: &'a [u8], // slice is fat pointer: { ptr: *const T, len: usize }
}

#[repr(C)]
pub struct VertexLayoutInfo<'a> {
    pub vertex_size: u64,
    pub attributes: &'a [wgpu::VertexAttribute], // slice is fat pointer: { ptr: *const T, len: usize }
}

// #[repr(C)]
// pub struct VertexAttrInfo {
//     pub format: wgpu::VertexFormat,
//     pub offset: wgpu::BufferAddress,
//     pub shader_location: wgpu::ShaderLocation,
// }

impl VertexLayoutInfo<'_> {
    // pub fn to_vertex_buffer_layout(&self) -> wgpu::VertexBufferLayout {
    //     let attrs: Vec<_> = self
    //         .attributes
    //         .as_slice()
    //         .iter()
    //         .map(|attr| wgpu::VertexAttribute {
    //             format: attr.format,
    //             offset: attr.offset,
    //             shader_location: attr.shader_location,
    //         })
    //         .collect();

    //     wgpu::VertexBufferLayout {
    //         array_stride: self.vertex_size,
    //         step_mode: wgpu::VertexStepMode::Vertex,
    //         attributes: attrs.as_slice(),
    //     }
    // }
    pub fn to_vertex_buffer_layout(&self) -> wgpu::VertexBufferLayout {
        wgpu::VertexBufferLayout {
            array_stride: self.vertex_size,
            step_mode: wgpu::VertexStepMode::Vertex,
            attributes: self.attributes,
        }
    }
}
