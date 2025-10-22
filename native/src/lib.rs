#![no_std]
use core::arch::asm;

#[unsafe(no_mangle)]
pub extern "C" fn get_frame_pointer() -> usize {
    let fp: usize;
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

#[panic_handler]
fn panic(_info: &core::panic::PanicInfo) -> ! {
    loop {}
}