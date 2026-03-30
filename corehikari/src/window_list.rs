use crate::screen::*;
use std::collections::HashMap;
use winit::window::WindowId;

#[derive(Debug)]
pub struct WindowList {
    window_map: HashMap<WindowId, WindowWrap>,
    screen_map: HashMap<ScreenId, ScreenWindow>,
}

impl WindowList {
    pub fn new() -> Self {
        Self {
            window_map: HashMap::new(),
            screen_map: HashMap::new(),
        }
    }

    pub fn is_empty(&self) -> bool {
        self.window_map.is_empty()
    }

    pub fn insert(&mut self, window_wrap: WindowWrap) {
        self.window_map.insert(window_wrap.window(), window_wrap);
        if let WindowWrap::ScreenWindow(screen_window) = window_wrap {
            self.screen_map.insert(screen_window.screen, screen_window);
        }
    }

    pub fn remove_by_screen(&mut self, screen_id: &ScreenId) {
        let removed = self.screen_map.remove(screen_id);
        if let Some(screen_window) = removed {
            self.window_map.remove(&screen_window.window);
        }
    }

    pub fn get(&self, window_id: &WindowId) -> Option<&WindowWrap> {
        self.window_map.get(window_id)
    }

    pub fn iter_screens(&self) -> impl Iterator<Item = &ScreenWindow> {
        self.screen_map.values()
    }
}

#[derive(Debug, Clone, Copy)]
pub enum WindowWrap {
    WindowOnly(WindowId),
    ScreenWindow(ScreenWindow),
}

impl WindowWrap {
    pub fn window(&self) -> WindowId {
        match self {
            WindowWrap::WindowOnly(window_id) => *window_id,
            WindowWrap::ScreenWindow(screen_window) => screen_window.window,
        }
    }
}

#[derive(Debug, Clone, Copy)]
pub struct ScreenWindow {
    pub window: WindowId,
    pub screen: ScreenId,
}
