#![no_std]
extern crate alloc;

use core::arch::asm;
use core::ffi::{c_char, c_void};

use libc_alloc::LibcAlloc;

#[global_allocator]
static ALLOCATOR: LibcAlloc = LibcAlloc;

#[unsafe(no_mangle)]
pub extern "C" fn get_frame_pointer() -> *const c_void {
    let fp: *const c_void;
    unsafe {
        #[cfg(target_arch = "x86_64")]
        asm!("mov {}, rbp", out(reg) fp);

        #[cfg(target_arch = "aarch64")]
        asm!("mov {}, x29", out(reg) fp);

        #[cfg(not(any(target_arch = "x86_64", target_arch = "aarch64")))]
        {
            fp = 0;
        }
    }
    fp
}

#[unsafe(no_mangle)]
pub static mut UNITY_LOG_CALLBACK: Option<extern "C" fn(*const c_char)> = None;

#[unsafe(no_mangle)]
pub extern "C" fn register_unity_logger(callback: extern "C" fn(*const c_char)) {
    unsafe { UNITY_LOG_CALLBACK = Some(callback); }
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn get_stack_trace(traces: *mut *mut c_void, traces_len: u8, start_offset: u8) -> u8 {
    let traces = core::slice::from_raw_parts_mut(traces, traces_len as usize);
    let mut depth = 0;
    let mut i = 0;
    unsafe {
        backtrace::trace_unsynchronized(|frame| {
            if depth >= start_offset {
                traces[i] = frame.ip();
                i += 1;
            }
            depth += 1;
            traces.len() > depth as usize
        });
    }

    // traces_len is limited to u8 too, so this is safe
    i as u8
}

#[panic_handler]
fn panic(info: &core::panic::PanicInfo) -> ! {
    unsafe {
        if let Some(cb) = UNITY_LOG_CALLBACK {
            let msg = alloc::format!("Rust panic: {}\0", info);
            // should really be a CString but whatever just testing
            cb(msg.as_ptr() as *const c_char);
        }
    }
    loop {}
}
