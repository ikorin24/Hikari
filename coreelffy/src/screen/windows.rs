#![cfg(target_os = "windows")]

use crate::screen::*;

pub(crate) fn create_window(
    config: &HostScreenConfig,
    event_loop: &event_loop::EventLoop<()>,
) -> Result<window::Window, Box<dyn Error>> {
    use winit::platform::windows::WindowBuilderExtWindows;

    let window = window::WindowBuilder::new()
        .with_title(config.title.as_str()?)
        .with_inner_size(dpi::Size::Physical(dpi::PhysicalSize::new(
            config.width,
            config.height,
        )))
        .with_theme(Some(window::Theme::Light))
        .build(&event_loop)?;
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
