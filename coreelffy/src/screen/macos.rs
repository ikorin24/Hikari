#![cfg(target_os = "macos")]

use crate::screen::*;

fn create_window(style: &WindowStyle) -> (window::Window, event_loop::EventLoop<()>) {
    todo!()
    // use winit::platform::macos::WindowExtMacOS;

    // let event_loop = event_loop::EventLoop::new();
    // let window = window::WindowBuilder::new()
    //     .with_title("Elffy")
    //     .with_inner_size(dpi::Size::Physical(dpi::PhysicalSize::new(1280, 720)))
    //     .build(&event_loop)
    //     .unwrap();
    // match style {
    //     WindowStyle::Default => {
    //         window.set_resizable(true);
    //     }
    //     WindowStyle::Fixed => {
    //         window.set_resizable(false);
    //     }
    //     WindowStyle::Fullscreen => {
    //         window.set_simple_fullscreen(true);
    //     }
    // }
    // (window, event_loop)
}
