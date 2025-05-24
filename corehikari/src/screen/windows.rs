#![cfg(target_os = "windows")]

use crate::screen::*;
use dpi::{PhysicalSize, Size};
use winit::{
    error::OsError,
    event_loop::ActiveEventLoop,
    window::{Fullscreen, Window, WindowButtons},
};

pub(crate) fn create_window(
    config: &ScreenConfig,
    event_loop: &ActiveEventLoop,
) -> Result<Window, OsError> {
    let window = event_loop.create_window(
        Window::default_attributes()
            .with_title("")
            .with_inner_size(Size::Physical(PhysicalSize::new(
                config.width,
                config.height,
            )))
            .with_min_inner_size(Size::Physical(PhysicalSize::new(1, 1)))
            .with_theme(None),
    )?;
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
