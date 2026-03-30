use crate::screen::*;
use crate::window_list::*;
use crate::*;
use std::cell::Cell;
use std::error::Error;
use std::fmt::Debug;
use std::sync::atomic::{AtomicBool, Ordering};
use std::sync::{Mutex, RwLock};
use winit;
use winit::application::ApplicationHandler;
use winit::event::{self, MouseScrollDelta, TouchPhase, WindowEvent};
use winit::event_loop::{ActiveEventLoop, ControlFlow, EventLoop, EventLoopProxy};
use winit::platform::run_on_demand::EventLoopExtRunOnDemand;
use winit::window::WindowId;

pub(crate) struct Engine {
    config: EngineCoreConfig,
    window_list: WindowList,
}

thread_local! {
    /// thread local error
    static TLS_LAST_ERROR: Cell<String>  = Cell::new("".to_owned());
}

pub(crate) fn set_tls_last_error(err: impl std::fmt::Display) {
    TLS_LAST_ERROR.with(|cell| cell.replace(format!("{}", err)));
}

pub(crate) fn take_tls_last_error() -> String {
    TLS_LAST_ERROR.with(|cell| cell.take())
}

pub(crate) fn get_tls_last_error_len() -> usize {
    TLS_LAST_ERROR.with(|cell| {
        let message = cell.take();
        let len = message.len();
        cell.set(message);
        len
    })
}

impl Engine {
    pub fn new(config: &EngineCoreConfig) -> Self {
        Engine {
            config: *config,
            window_list: WindowList::new(),
        }
    }

    fn on_screen_init(&self, screen: Box<Screen>) -> ScreenId {
        let f = self.config.on_screen_init;
        let screen_info = &screen.get_info();
        f(screen, screen_info)
    }

    fn on_unhandled_error(&self) -> impl Fn(&str) + Send + Sync + 'static {
        let f = self.config.on_unhandled_error;
        move |error: &str| {
            let bytes = error.as_bytes();
            f(bytes.as_ptr(), bytes.len())
        }
    }

    fn event_cleared(&self, screen_id: ScreenId) {
        let f = self.config.event_cleared;
        f(screen_id)
    }

    fn event_redraw_requested(&self, screen_id: ScreenId) -> bool {
        let f = self.config.event_redraw_requested;
        f(screen_id)
    }

    fn event_resized(&self, screen_id: ScreenId, width: u32, height: u32) {
        let f = self.config.event_resized;
        f(screen_id, width, height)
    }

    fn event_keyboard(&self, screen_id: ScreenId, key: crate::KeyCode, pressed: bool) {
        let f = self.config.event_keyboard;
        f(screen_id, key, pressed);
    }

    fn event_char_received(&self, screen_id: ScreenId, c: char) {
        let f = self.config.event_char_received;
        f(screen_id, c as u32)
    }

    fn event_mouse_button(
        &self,
        screen_id: ScreenId,
        button: &event::MouseButton,
        state: &event::ElementState,
    ) {
        let f = self.config.event_mouse_button;
        let pressed = match state {
            event::ElementState::Pressed => true,
            event::ElementState::Released => false,
        };
        f(screen_id, (*button).into(), pressed)
    }

    fn event_ime(&self, screen_id: ScreenId, input: &ImeInputData) {
        let f = self.config.event_ime;
        f(screen_id, input)
    }

    fn event_wheel(&self, screen_id: ScreenId, x_delta: f32, y_delta: f32) {
        let f = self.config.event_wheel;
        f(screen_id, x_delta, y_delta)
    }

    fn event_cursor_moved(&self, screen_id: ScreenId, x: f32, y: f32) {
        let f = self.config.event_cursor_moved;
        f(screen_id, x, y)
    }

    fn event_cursor_entered_left(&self, screen_id: ScreenId, entered: bool) {
        let f = self.config.event_cursor_entered_left;
        f(screen_id, entered);
    }

    fn event_closing(&self, screen_id: ScreenId) -> bool {
        let f = self.config.event_closing;
        let mut cancel = false;
        f(screen_id, &mut cancel);
        !cancel
    }

    fn event_closed(&self, screen_id: ScreenId) -> Option<Box<Screen>> {
        let f = self.config.event_closed;
        f(screen_id)
    }

    fn close_screen(&mut self, screen_id: ScreenId) -> bool {
        if self.event_closing(screen_id) {
            self.window_list.remove_by_screen(&screen_id);
            let is_empty = self.window_list.is_empty();
            let closed_screen = self.event_closed(screen_id);
            drop(closed_screen);
            is_empty
        } else {
            false
        }
    }
}

impl ApplicationHandler<ProxyMessage> for Engine {
    fn resumed(&mut self, _event_loop: &ActiveEventLoop) {}

    fn user_event(&mut self, event_loop: &ActiveEventLoop, event: ProxyMessage) {
        match event {
            ProxyMessage::CreateScreen(config) => {
                let screen = match Screen::new(&config, event_loop, self.on_unhandled_error()) {
                    Ok(screen) => screen,
                    Err(_err) => {
                        return;
                    }
                };
                let screen = Box::new(screen);
                let window_id = screen.window.id();
                let screen_id = self.on_screen_init(screen);
                self.window_list
                    .insert(WindowWrap::ScreenWindow(ScreenWindow {
                        window: window_id,
                        screen: screen_id,
                    }));
            }
        }
    }

    fn about_to_wait(&mut self, _event_loop: &ActiveEventLoop) {
        self.window_list.iter_screens().for_each(|screen_window| {
            self.event_cleared(screen_window.screen);
        });
    }

    fn window_event(
        &mut self,
        event_loop: &ActiveEventLoop,
        window_id: WindowId,
        event: WindowEvent,
    ) {
        let screen = match self.window_list.get(&window_id) {
            Some(WindowWrap::ScreenWindow(x)) => x.screen,
            Some(WindowWrap::WindowOnly(_)) => {
                return;
            }
            None => {
                return;
            }
        };
        match event {
            WindowEvent::CursorEntered { .. } => {
                self.event_cursor_entered_left(screen, true);
            }
            WindowEvent::CursorLeft { .. } => {
                self.event_cursor_entered_left(screen, false);
            }
            WindowEvent::CursorMoved { position, .. } => {
                self.event_cursor_moved(screen, position.x as f32, position.y as f32);
            }
            WindowEvent::Ime(ime) => {
                let data = ImeInputData::new(&ime);
                self.event_ime(screen, &data);
            }
            WindowEvent::MouseInput { state, button, .. } => {
                self.event_mouse_button(screen, &button, &state);
            }
            WindowEvent::MouseWheel { delta, phase, .. } if phase == TouchPhase::Moved => {
                match delta {
                    MouseScrollDelta::LineDelta(x_delta, y_delta) => {
                        self.event_wheel(screen, x_delta, y_delta);
                    }
                    MouseScrollDelta::PixelDelta(_pos) => {
                        // TODO: support touchpad devices
                    }
                }
            }
            WindowEvent::CloseRequested => {
                if self.close_screen(screen) {
                    event_loop.exit();
                }
            }
            WindowEvent::KeyboardInput { event, .. } => {
                use winit::keyboard::PhysicalKey;
                if let PhysicalKey::Code(keycode) = &event.physical_key {
                    if let Ok(key) = keycode.try_into() {
                        let pressed = match event.state {
                            event::ElementState::Pressed => true,
                            event::ElementState::Released => false,
                        };
                        self.event_keyboard(screen, key, pressed);
                    }
                }

                if let Some(text) = event.text.as_ref() {
                    let text = text.as_ref();
                    if text.is_empty() == false {
                        text.chars().for_each(|c| {
                            self.event_char_received(screen, c);
                        });
                    }
                }
            }
            WindowEvent::Resized(physical_size) => {
                self.event_resized(screen, physical_size.width, physical_size.height);
            }
            // WindowEvent::ScaleFactorChanged { new_inner_size, .. } => {
            //     Engine::event_resized(screen, new_inner_size.width, new_inner_size.height);
            // }
            WindowEvent::ScaleFactorChanged {
                scale_factor,
                inner_size_writer,
            } => {
                _ = scale_factor;
                _ = inner_size_writer;
                // TODO: 分からん
                // Engine::event_resized(screen, new_inner_size.width, new_inner_size.height);
            }
            WindowEvent::RedrawRequested => {
                let continue_next = self.event_redraw_requested(screen);
                if continue_next == false {
                    if self.close_screen(screen) {
                        event_loop.exit();
                    }
                }
            }
            _ => {}
        }
    }
}

static LOOP_PROXY: Mutex<Option<EventLoopProxy<ProxyMessage>>> = Mutex::new(None);

#[derive(Debug, Clone, Copy)]
pub(crate) enum ProxyMessage {
    CreateScreen(ScreenConfig),
}

pub(crate) fn get_loop_proxy() -> Result<EventLoopProxy<ProxyMessage>, EngineErr> {
    let proxy = LOOP_PROXY.lock().unwrap();
    proxy.clone().ok_or(EngineErr::NOT_RUNNING)
}

pub(crate) fn send_proxy_message(message: ProxyMessage) -> Result<(), Box<dyn Error>> {
    let proxy = get_loop_proxy()?;
    proxy.send_event(message)?;
    Ok(())
}

pub(crate) static DEBUG_PRINTLN: RwLock<Option<DebugPrintlnFn>> = RwLock::new(None);

#[macro_export]
macro_rules! debug_println {
    ($($arg:tt)*) => {
        if let Some(ref f) = *crate::engine::DEBUG_PRINTLN.read().unwrap() {
            let message: String = format!($($arg)*);
            f(message.as_ptr(), message.len());
            true
        } else {
            false
        }
    };
}

static IS_ENGINE_RUNNING: AtomicBool = AtomicBool::new(false);

pub(crate) fn engine_start(
    engine_config: &EngineCoreConfig,
    screen_config: &ScreenConfig,
) -> Result<(), Box<dyn Error>> {
    if IS_ENGINE_RUNNING.swap(true, Ordering::Relaxed) {
        return Err(EngineErr::ALREADY_RUNNING.into());
    }
    DEBUG_PRINTLN
        .write()
        .unwrap()
        .replace(engine_config.debug_println);
    let mut event_loop = EventLoop::with_user_event().build()?;
    event_loop.set_control_flow(ControlFlow::Poll);

    debug_println!("[corehikari] engine start");
    let mut engine = Engine::new(engine_config);
    {
        let mut proxy = LOOP_PROXY.lock().unwrap();
        *proxy = Some(event_loop.create_proxy());
    }
    env_logger::init();

    send_proxy_message(ProxyMessage::CreateScreen(*screen_config))?;
    event_loop.run_app_on_demand(&mut engine)?;
    IS_ENGINE_RUNNING.store(false, Ordering::Relaxed);

    debug_println!("[corehikari] engine stop");
    DEBUG_PRINTLN.write().unwrap().take();
    Ok(())
}

#[derive(Clone, Copy)]
pub(crate) struct EngineErr {
    message: &'static str,
}

impl std::fmt::Display for EngineErr {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(f, "{}", self.message)
    }
}

impl std::error::Error for EngineErr {
    fn source(&self) -> Option<&(dyn Error + 'static)> {
        None
    }

    fn description(&self) -> &str {
        "description() is deprecated; use Display"
    }

    fn cause(&self) -> Option<&dyn Error> {
        self.source()
    }
}

impl Debug for EngineErr {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        f.debug_struct("EngineErr")
            .field("message", &self.message)
            .finish()
    }
}

impl EngineErr {
    pub const fn new(message: &'static str) -> Self {
        Self { message }
    }

    const NOT_RUNNING: Self = Self::new("The engine is not running");
    const ALREADY_RUNNING: Self = Self::new("The engine is already running");
}

#[repr(transparent)]
pub(crate) struct ApiResult {
    #[allow(dead_code)]
    success: bool,
}

impl ApiResult {
    #[inline]
    pub fn ok_or_set_error<E: std::fmt::Display>(result: Result<(), E>) -> Self {
        match result {
            Ok(_) => Self::ok(),
            Err(err) => {
                set_tls_last_error(err);
                Self::err()
            }
        }
    }

    pub const fn ok() -> Self {
        Self { success: true }
    }

    pub const fn err() -> Self {
        Self { success: false }
    }
}

#[repr(C)]
pub(crate) struct ApiBoxResult<T> {
    success: bool,
    value: Option<Box<T>>,
}

impl<T> ApiBoxResult<T> {
    #[inline]
    pub fn ok_or_set_error<E: std::fmt::Display>(result: Result<Box<T>, E>) -> Self {
        match result {
            Ok(value) => Self::ok(value),
            Err(err) => {
                set_tls_last_error(err);
                Self::err()
            }
        }
    }

    #[inline]
    pub const fn ok(value: Box<T>) -> Self {
        Self {
            success: true,
            value: Some(value),
        }
    }

    #[inline]
    pub fn err() -> Self {
        Self {
            success: false,
            value: None,
        }
    }
}

#[repr(C)]
pub(crate) struct ApiValueResult<T: Default + 'static> {
    success: bool,
    value: T,
}

impl<T: Default> ApiValueResult<T> {
    #[inline]
    pub fn ok_or_set_error<E: std::fmt::Display>(result: Result<T, E>) -> Self {
        match result {
            Ok(value) => Self::ok(value),
            Err(err) => {
                set_tls_last_error(err);
                Self::err()
            }
        }
    }

    #[inline]
    pub const fn ok(value: T) -> Self {
        Self {
            success: true,
            value,
        }
    }
    #[inline]
    pub fn err() -> Self {
        Self {
            success: false,
            value: Default::default(),
        }
    }
}

impl<T: Default, E> From<Result<T, E>> for ApiValueResult<T>
where
    E: std::fmt::Display,
{
    fn from(result: Result<T, E>) -> Self {
        Self::ok_or_set_error(result)
    }
}
