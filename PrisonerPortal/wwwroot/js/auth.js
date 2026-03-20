document.addEventListener('DOMContentLoaded', () => {
    initPageAnimations();
    initPasswordToggles();
    initPasswordStrength();
    initEmailCheck();
});

// 1. Toggle Password Visibility
function initPasswordToggles() {
    document.querySelectorAll('.password-toggle').forEach(toggle => {
        toggle.addEventListener('click', function() {
            const inputId = this.getAttribute('data-target');
            const input = document.getElementById(inputId);
            const icon = this.querySelector('i');
            
            if (input.type === 'password') {
                input.type = 'text';
                icon.classList.replace('fa-eye', 'fa-eye-slash');
            } else {
                input.type = 'password';
                icon.classList.replace('fa-eye-slash', 'fa-eye');
            }
        });
    });
}

// 2. Password Strength Indicator
function initPasswordStrength() {
    const passwordInput = document.getElementById('RegisterPassword');
    if (!passwordInput) return;

    passwordInput.addEventListener('input', function() {
        const val = this.value;
        const score = checkPasswordStrength(val);
        updateStrengthUI(score);
        updateRequirements(val);
    });
}

function checkPasswordStrength(password) {
    let score = 0;
    if (!password) return score;
    if (password.length >= 8) score++;
    if (/[A-Z]/.test(password)) score++;
    if (/[0-9]/.test(password)) score++;
    if (/[@$!%*?&]/.test(password)) score++;
    return score;
}

function updateStrengthUI(score) {
    const bars = document.querySelectorAll('.strength-bar');
    const label = document.getElementById('strength-label');
    
    bars.forEach((bar, index) => {
        bar.className = 'strength-bar'; // Reset
        if (index < score) {
            if (score <= 1) bar.classList.add('bg-danger');
            else if (score === 2) bar.classList.add('bg-warning');
            else if (score === 3) bar.classList.add('bg-info');
            else bar.classList.add('bg-success');
        }
    });

    const texts = ["Weak", "Fair", "Good", "Strong"];
    if (label) label.textContent = score > 0 ? texts[score - 1] : "";
}

function updateRequirements(password) {
    const reqs = {
        'req-len': password.length >= 8,
        'req-upper': /[A-Z]/.test(password),
        'req-num': /[0-9]/.test(password),
        'req-spec': /[@$!%*?&]/.test(password)
    };

    for (const [id, met] of Object.entries(reqs)) {
        const el = document.getElementById(id);
        if (el) {
            const icon = el.querySelector('i');
            if (met) {
                el.classList.add('text-success');
                el.classList.remove('text-muted');
                icon.className = 'fa-solid fa-check me-2';
            } else {
                el.classList.remove('text-success');
                el.classList.add('text-muted');
                icon.className = 'fa-solid fa-xmark me-2';
            }
        }
    }
}

// 3. AJAX Email Check
function initEmailCheck() {
    const emailInput = document.getElementById('RegisterEmail');
    if (!emailInput) return;

    emailInput.addEventListener('blur', async function() {
        const email = this.value;
        if (!email || !email.includes('@')) return;

        try {
            const resp = await fetch(`/Account/CheckEmail?email=${encodeURIComponent(email)}`);
            const data = await resp.json();
            if (data.exists) {
                showFieldError('RegisterEmail', 'This email is already registered. Login instead?');
            } else {
                clearFieldError('RegisterEmail');
            }
        } catch (err) {
            console.error('Email check failed', err);
        }
    });
}

// 4. Multi-step Registration Navigation
let currentStep = 1;

window.nextStep = function() {
    if (validateStep1()) {
        const s1 = document.getElementById('step-1');
        const s2 = document.getElementById('step-2');
        const progress = document.getElementById('progress-line-fill');
        const dot2 = document.getElementById('step-dot-2');

        s1.classList.add('slide-out-left');
        s2.classList.remove('d-none');
        s2.classList.add('slide-in-right');
        
        progress.style.width = '100%';
        dot2.classList.add('active');
        
        setTimeout(() => {
            s1.classList.add('d-none');
            s1.classList.remove('slide-out-left');
        }, 350);
        
        currentStep = 2;
    }
};

window.prevStep = function() {
    const s1 = document.getElementById('step-1');
    const s2 = document.getElementById('step-2');
    const progress = document.getElementById('progress-line-fill');
    const dot2 = document.getElementById('step-dot-2');

    s2.classList.add('slide-out-right');
    s1.classList.remove('d-none');
    s1.classList.add('slide-in-left');
    
    progress.style.width = '50%';
    dot2.classList.remove('active');
    
    setTimeout(() => {
        s2.classList.add('d-none');
        s2.classList.remove('slide-out-right');
        s1.classList.remove('slide-in-left');
    }, 350);
    
    currentStep = 1;
};

// 5. Client-Side Validation
function validateStep1() {
    let valid = true;
    const name = document.getElementById('FullName');
    const email = document.getElementById('RegisterEmail');
    const phone = document.getElementById('PhoneNumber');
    const address = document.getElementById('Address');

    if (!name.value || name.value.length < 3) {
        showFieldError('FullName', 'Full name is required (min 3 chars)');
        valid = false;
    } else {
        clearFieldError('FullName');
    }

    if (!email.value || !email.value.includes('@')) {
        showFieldError('RegisterEmail', 'Enter a valid email address');
        valid = false;
    } else {
        // Clear if not already showing "exists" error
        const err = email.closest('.form-field-wrapper').querySelector('.error-msg');
        if (err.textContent !== 'This email is already registered. Login instead?') {
            clearFieldError('RegisterEmail');
        } else {
            valid = false;
        }
    }

    if (!phone.value || !/^\d{10}$/.test(phone.value)) {
        showFieldError('PhoneNumber', 'Enter a valid 10-digit mobile number');
        valid = false;
    } else {
        clearFieldError('PhoneNumber');
    }

    if (!address.value || address.value.length < 10) {
        showFieldError('Address', 'Please enter your full address (min 10 chars)');
        valid = false;
    } else {
        clearFieldError('Address');
    }

    if (!valid) {
        document.querySelector('.auth-right-panel').scrollTo({ top: 0, behavior: 'smooth' });
    }

    return valid;
}

function showFieldError(fieldId, message) {
    const field = document.getElementById(fieldId);
    const wrapper = field.closest('.form-field-wrapper');
    wrapper.classList.add('has-error');
    const errorMsg = wrapper.querySelector('.error-msg');
    errorMsg.textContent = message;
    errorMsg.classList.remove('d-none');
    
    // Shake animation
    wrapper.classList.add('shake');
    setTimeout(() => wrapper.classList.remove('shake'), 400);
}

function clearFieldError(fieldId) {
    const field = document.getElementById(fieldId);
    const wrapper = field.closest('.form-field-wrapper');
    wrapper.classList.remove('has-error');
    const errorMsg = wrapper.querySelector('.error-msg');
    errorMsg.textContent = "";
    errorMsg.classList.add('d-none');
}

// 6. Page Animations Init
function initPageAnimations() {
    const left = document.querySelector('.auth-left-panel');
    const right = document.querySelector('.auth-right-panel');
    const fields = document.querySelectorAll('.animate-field');

    if (left) left.classList.add('animate-in');
    if (right) right.classList.add('animate-in');

    fields.forEach((field, i) => {
        setTimeout(() => {
            field.classList.add('active');
        }, 300 + (i * 100));
    });
}
