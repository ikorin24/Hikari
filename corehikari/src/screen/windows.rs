#![cfg(target_os = "windows")]

use crate::screen::*;
use dpi::{PhysicalSize, Size};
use winit::window::{Fullscreen, WindowButtons};

pub(crate) fn create_window(
    config: &ScreenConfig,
    event_loop: &EventLoopWindowTarget<ProxyMessage>,
) -> Result<window::Window, Box<dyn Error>> {
    let window = window::WindowBuilder::new()
        .with_title("")
        .with_inner_size(Size::Physical(dpi::PhysicalSize::new(
            config.width,
            config.height,
        )))
        .with_min_inner_size(Size::Physical(PhysicalSize::new(1, 1)))
        .with_theme(None)
        .build(event_loop)?;
    match config.style {
        WindowStyle::Default => {
            window.set_resizable(true);
            window.set_enabled_buttons(WindowButtons::all())
        }
        WindowStyle::Fixed => {
            window.set_resizable(false);
            window.set_enabled_buttons(WindowButtons::CLOSE | WindowButtons::MINIMIZE);
        }
        WindowStyle::Fullscreen => {
            window.set_fullscreen(Some(Fullscreen::Borderless(None)));
        }
    }
    Ok(window)
}
