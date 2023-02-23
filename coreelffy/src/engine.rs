use crate::error_handler::*;
use crate::screen::*;
use crate::*;
use std::error::Error;
use std::fmt::Debug;
use std::sync;
use winit;
use winit::platform::run_return::EventLoopExtRunReturn;
use winit::{event, event_loop, window};

pub(crate) struct Engine {
    on_screen_init: HostScreenInitFn,
    event_cleared: ClearedEventFn,
    event_redraw_requested: RedrawRequestedEventFn,
    event_resized: ResizedEventFn,
    event_keyboard: KeyboardEventFn,
    event_char_received: CharReceivedEventFn,
    event_closing: ClosingEventFn,
}

impl Engine {
    pub fn new(config: &EngineCoreConfig) -> Self {
        Engine {
            on_screen_init: config.on_screen_init,
            event_cleared: config.event_cleared,
            event_redraw_requested: config.event_redraw_requested,
            event_resized: config.event_resized,
            event_keyboard: config.event_keyboard,
            event_char_received: config.event_char_received,
            event_closing: config.event_closing,
        }
    }

    pub fn on_screen_init(screen: Box<HostScreen>) -> ScreenId {
        let f = Self::get_callback(|engine| engine.on_screen_init).unwrap();
        let screen_info = &screen.get_info();
        f(screen, screen_info)
    }

    pub fn event_cleared(screen_id: ScreenId) {
        let f = Self::get_callback(|engine| engine.event_cleared).unwrap();
        f(screen_id)
    }

    pub fn event_redraw_requested(screen_id: ScreenId) -> bool {
        let f = Self::get_callback(|engine| engine.event_redraw_requested).unwrap();
        f(screen_id)
    }

    pub fn event_resized(screen_id: ScreenId, width: u32, height: u32) {
        let f = Self::get_callback(|engine| engine.event_resized).unwrap();
        f(screen_id, width, height)
    }

    pub fn event_keyboard(
        screen_id: ScreenId,
        key: &event::VirtualKeyCode,
        state: &event::ElementState,
    ) {
        let f = Self::get_callback(|engine| engine.event_keyboard).unwrap();
        let pressed = match state {
            event::ElementState::Pressed => true,
            event::ElementState::Released => false,
        };
        f(screen_id, *key, pressed)
    }

    pub fn event_char_received(screen_id: ScreenId, c: char) {
        let f = Self::get_callback(|engine| engine.event_char_received).unwrap();
        f(screen_id, c)
    }

    pub fn event_closing(screen_id: ScreenId) -> bool {
        let f = Self::get_callback(|engine| engine.event_closing).unwrap();
        let mut cancel = false;
        f(screen_id, &mut cancel);
        !cancel
    }

    fn get_callback<T>(f: fn(engine: &Engine) -> T) -> Result<T, EngineErr> {
        let reader = ENGINE.read().unwrap();
        match reader.as_ref().map(f) {
            Some(value) => Ok(value),
            None => Err(Self::SHOULD_NOT_NONE_WHEN_ENGINE_RUNNING),
        }
    }

    const SHOULD_NOT_NONE_WHEN_ENGINE_RUNNING: EngineErr =
        EngineErr::new("It should not be None when engine is running");
}

static ENGINE: sync::RwLock<Option<Engine>> = sync::RwLock::new(None);

#[derive(Clone, Copy)]
struct ScreenInfo(window::WindowId, ScreenId);

pub(crate) fn engine_start(
    engine_config: &EngineCoreConfig,
    screen_config: &HostScreenConfig,
) -> Result<(), Box<dyn Error>> {
    env_logger::init();
    set_err_dispatcher(engine_config.err_dispatcher);
    {
        let mut engine = ENGINE.write()?;
        *engine = Some(Engine::new(engine_config));
    }
    let mut event_loop = event_loop::EventLoop::new();
    let mut screens: Vec<ScreenInfo> = vec![];

    let screen = Box::new(HostScreen::new(screen_config, &event_loop)?);
    let window_id = screen.window.id();
    let screen_id = Engine::on_screen_init(screen);
    screens.push(ScreenInfo(window_id, screen_id));
    let screen_info = screens.last().unwrap();

    // let find_screen = engine_config.on_find_screen;

    event_loop.run_return(move |event, _event_loop, control_flow| {
        let continue_next = handle_event(screen_info, &event);
        if continue_next == false {
            if Engine::event_closing(screen_id) {
                *control_flow = winit::event_loop::ControlFlow::Exit;
            }
        }
    });
    return Ok(());
}

fn handle_event(target: &ScreenInfo, event: &event::Event<()>) -> bool {
    use winit::event::*;

    match event {
        Event::WindowEvent {
            ref event,
            window_id,
        } if *window_id == target.0 => match event {
            WindowEvent::ReceivedCharacter(c) => {
                Engine::event_char_received(target.1, *c);
                true
            }
            WindowEvent::CloseRequested => false,
            WindowEvent::KeyboardInput {
                input:
                    KeyboardInput {
                        state,
                        virtual_keycode: Some(key),
                        ..
                    },
                ..
            } => {
                Engine::event_keyboard(target.1, key, state);
                true
            }
            WindowEvent::Resized(physical_size) => {
                Engine::event_resized(target.1, physical_size.width, physical_size.height);
                true
            }
            WindowEvent::ScaleFactorChanged { new_inner_size, .. } => {
                Engine::event_resized(target.1, new_inner_size.width, new_inner_size.height);
                true
            }
            _ => true,
        },
        Event::RedrawRequested(window_id) if *window_id == target.0 => {
            Engine::event_redraw_requested(target.1)
        }
        Event::MainEventsCleared => {
            Engine::event_cleared(target.1);
            true
        }
        _ => true,
    }
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
}
