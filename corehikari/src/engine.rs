use crate::screen::*;
use crate::*;
use std::cell::Cell;
use std::error::Error;
use std::fmt::Debug;
use std::sync::atomic::{AtomicBool, Ordering};
use std::sync::Mutex;
use winit;
use winit::application::ApplicationHandler;
use winit::event::{MouseScrollDelta, TouchPhase, WindowEvent};
use winit::event_loop::{ActiveEventLoop, ControlFlow, EventLoop, EventLoopProxy};
use winit::platform::run_on_demand::EventLoopExtRunOnDemand;
use winit::window::WindowId;
use winit::{event, window};

pub(crate) struct Engine {
    config: EngineCoreConfig,
    screens: Vec<ScreenIdData>,
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
            screens: vec![],
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

    // pub fn event_keyboard(
    //     screen_id: ScreenId,
    //     key: &event::VirtualKeyCode,
    //     state: &event::ElementState,
    // ) {
    //     let f = Self::get_engine_field(|engine| engine.config.event_keyboard).unwrap();
    //     let pressed = match state {
    //         event::ElementState::Pressed => true,
    //         event::ElementState::Released => false,
    //     };
    //     f(screen_id, *key, pressed)
    // }

    fn event_char_received(&self, screen_id: ScreenId, c: char) {
        let f = self.config.event_char_received;
        f(screen_id, c)
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

    fn close_screen(&mut self, target: &ScreenIdData) -> bool {
        if self.event_closing(target.1) {
            let index = self.screens.iter().position(|x| x == target);
            if let Some(index) = index {
                self.screens.swap_remove(index);
            }
            let is_empty = self.screens.is_empty();
            let closed_screen = self.event_closed(target.1);
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
                self.screens.push(ScreenIdData(window_id, screen_id));
            }
        }
    }

    fn about_to_wait(&mut self, _event_loop: &ActiveEventLoop) {
        self.screens.iter().for_each(|x| {
            self.event_cleared(x.1);
        });
    }

    fn window_event(
        &mut self,
        event_loop: &ActiveEventLoop,
        window_id: WindowId,
        event: WindowEvent,
    ) {
        let target = match self.screens.iter().find(|x| x.0 == window_id).copied() {
            Some(target) => target,
            None => {
                return;
            }
        };
        match event {
            WindowEvent::CursorEntered { .. } => {
                self.event_cursor_entered_left(target.1, true);
            }
            WindowEvent::CursorLeft { .. } => {
                self.event_cursor_entered_left(target.1, false);
            }
            WindowEvent::CursorMoved { position, .. } => {
                self.event_cursor_moved(target.1, position.x as f32, position.y as f32);
            }
            WindowEvent::Ime(ime) => {
                let data = ImeInputData::new(&ime);
                self.event_ime(target.1, &data);
            }
            // WindowEvent::ReceivedCharacter(c) => {
            //     Engine::event_char_received(target.1, *c);
            // }
            WindowEvent::MouseInput { state, button, .. } => {
                self.event_mouse_button(target.1, &button, &state);
            }
            WindowEvent::MouseWheel { delta, phase, .. } if phase == TouchPhase::Moved => {
                match delta {
                    MouseScrollDelta::LineDelta(x_delta, y_delta) => {
                        self.event_wheel(target.1, x_delta, y_delta);
                    }
                    MouseScrollDelta::PixelDelta(_pos) => {
                        // TODO: support touchpad devices
                    }
                }
            }
            WindowEvent::CloseRequested => {
                if self.close_screen(&target) {
                    event_loop.exit();
                }
            }
            // WindowEvent::KeyboardInput {
            //     input:
            //         KeyboardInput {
            //             state,
            //             virtual_keycode: Some(key),
            //             ..
            //         },
            //     ..
            // } => {
            //     Engine::event_keyboard(target.1, key, state);
            // }
            WindowEvent::KeyboardInput {
                device_id,
                event,
                is_synthetic,
            } => {
                _ = device_id;
                _ = event;
                _ = is_synthetic;
                // TODO
                // Engine::event_keyboard(target.1, key, &event.state);
            }
            WindowEvent::Resized(physical_size) => {
                self.event_resized(target.1, physical_size.width, physical_size.height);
            }
            // WindowEvent::ScaleFactorChanged { new_inner_size, .. } => {
            //     Engine::event_resized(target.1, new_inner_size.width, new_inner_size.height);
            // }
            WindowEvent::ScaleFactorChanged {
                scale_factor,
                inner_size_writer,
            } => {
                _ = scale_factor;
                _ = inner_size_writer;
                // TODO: 分からん
                // Engine::event_resized(target.1, new_inner_size.width, new_inner_size.height);
            }
            WindowEvent::RedrawRequested => {
                let continue_next = self.event_redraw_requested(target.1);
                if continue_next == false {
                    if self.close_screen(&target) {
                        event_loop.exit();
                    }
                }
            }
            // WindowEvent::MainEventsCleared => {
            //     let screens = SCREENS.lock().unwrap();
            //     screens.iter().for_each(|x| {
            //         Engine::event_cleared(x.1);
            //     });
            // }
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

#[derive(Clone, Copy, PartialEq, Eq, Hash)]
struct ScreenIdData(window::WindowId, ScreenId);

pub(crate) fn send_proxy_message(message: ProxyMessage) -> Result<(), Box<dyn Error>> {
    let proxy = get_loop_proxy()?;
    proxy.send_event(message)?;
    Ok(())
}

static IS_ENGINE_RUNNING: AtomicBool = AtomicBool::new(false);

pub(crate) fn engine_start(
    engine_config: &EngineCoreConfig,
    screen_config: &ScreenConfig,
) -> Result<(), Box<dyn Error>> {
    if IS_ENGINE_RUNNING.swap(true, Ordering::Relaxed) {
        return Err(EngineErr::ALREADY_RUNNING.into());
    }
    let mut event_loop = EventLoop::with_user_event().build()?;
    event_loop.set_control_flow(ControlFlow::Poll);
    let mut engine = Engine::new(engine_config);
    {
        let mut proxy = LOOP_PROXY.lock().unwrap();
        *proxy = Some(event_loop.create_proxy());
    }
    env_logger::init();

    get_loop_proxy()
        .unwrap()
        .send_event(ProxyMessage::CreateScreen(*screen_config))?;
    event_loop.run_app_on_demand(&mut engine)?;
    IS_ENGINE_RUNNING.store(false, Ordering::Relaxed);
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
