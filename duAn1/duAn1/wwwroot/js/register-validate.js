const validateRegister = () => {
    const name = document.getElementById('name').value.trim();
    const email = document.getElementById('email').value.trim();
    const password = document.getElementById('password').value;
    const confirmPassword = document.getElementById('confirmPassword').value;
    const emailPattern = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;

    // Reset lỗi
    document.getElementById('nameError').style.display = 'none';
    document.getElementById('emailError').style.display = 'none';
    document.getElementById('passwordError').style.display = 'none';
    document.getElementById('confirmPasswordError').style.display = 'none';

    let valid = true;

    if (!name) {
        document.getElementById('nameError').innerHTML = 'Vui lòng nhập họ và tên.';
        document.getElementById('nameError').style.display = 'block';
        valid = false;
    }
    if (!email) {
        document.getElementById('emailError').innerHTML = 'Vui lòng nhập email.';
        document.getElementById('emailError').style.display = 'block';
        valid = false;
    } else if (!emailPattern.test(email)) {
        document.getElementById('emailError').innerHTML = 'Email không hợp lệ.';
        document.getElementById('emailError').style.display = 'block';
        valid = false;
    }
    if (!password) {
        document.getElementById('passwordError').innerHTML = 'Vui lòng nhập mật khẩu.';
        document.getElementById('passwordError').style.display = 'block';
        valid = false;
    }
    if (!confirmPassword) {
        document.getElementById('confirmPasswordError').innerHTML = 'Vui lòng xác nhận mật khẩu.';
        document.getElementById('confirmPasswordError').style.display = 'block';
        valid = false;
    } else if (password && confirmPassword && password !== confirmPassword) {
        document.getElementById('confirmPasswordError').innerHTML = 'Mật khẩu xác nhận không khớp.';
        document.getElementById('confirmPasswordError').style.display = 'block';
        valid = false;
    }
    return valid;
};
