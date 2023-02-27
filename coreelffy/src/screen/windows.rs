#![cfg(target_os = "windows")]

use crate::screen::*;

pub(crate) fn create_window(
    config: &HostScreenConfig,
    event_loop: &EventLoopWindowTarget<ProxyMessage>,
) -> Result<window::Window, Box<dyn Error>> {
    let window = window::WindowBuilder::new()
        .with_title("")
        .with_inner_size(dpi::Size::Physical(dpi::PhysicalSize::new(
            config.width,
            config.height,
        )))
        .with_theme(Some(window::Theme::Light))
        .build(event_loop)?;
    match config.style {
        WindowStyle::Default => {
            window.set_resizable(true);
        }
        WindowStyle::Fixed => {
            window.set_resizable(false);
        }
        WindowStyle::Fullscreen => {
            window.set_fullscreen(None);
        }
    }
    Ok(window)
}
