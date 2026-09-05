export function initBadgeInteractions(dotNetHelper) {
    // 1. DRAG REALISTA COM INÉRCIA (Para a grelha de Badges)
    const badgeGrid = document.getElementById('badge-grid');
    if (badgeGrid && !badgeGrid.dataset.dragInit) {
        let isDown = false;
        let startX;
        let scrollLeft;
        let velocity = 0;
        let lastX = 0;
        let animationFrame;

        // Função de inércia contínua após largar o rato
        function beginInertia() {
            function step() {
                if (Math.abs(velocity) > 0.5) {
                    badgeGrid.scrollLeft -= velocity;
                    velocity *= 0.92; // Multiplicador de atrito (quanto mais perto de 1, mais escorrega)
                    animationFrame = requestAnimationFrame(step);
                }
            }
            animationFrame = requestAnimationFrame(step);
        }

        badgeGrid.addEventListener('mousedown', (e) => {
            isDown = true;
            badgeGrid.style.cursor = 'grabbing';
            cancelAnimationFrame(animationFrame); // Pára qualquer inércia anterior
            startX = e.pageX - badgeGrid.offsetLeft;
            scrollLeft = badgeGrid.scrollLeft;
            lastX = e.pageX;
            velocity = 0;
        });

        badgeGrid.addEventListener('mouseleave', () => {
            if (!isDown) return;
            isDown = false;
            badgeGrid.style.cursor = 'pointer';
            beginInertia();
        });

        badgeGrid.addEventListener('mouseup', () => {
            isDown = false;
            badgeGrid.style.cursor = 'pointer';
            beginInertia();
        });

        badgeGrid.addEventListener('mousemove', (e) => {
            if (!isDown) return;
            e.preventDefault();
            const currentX = e.pageX;
            const x = currentX - badgeGrid.offsetLeft;
            const walk = (x - startX);
            badgeGrid.scrollLeft = scrollLeft - walk;

            velocity = currentX - lastX; // Calcula a força do movimento
            lastX = currentX;
        });

        badgeGrid.dataset.dragInit = "true";
    }

    // 2. TILT EFFECT 3D (Apenas Desktop)
    const isDesktop = window.matchMedia("(hover: hover) and (pointer: fine)").matches;
    const cards = document.querySelectorAll('.badge-card-heavy');

    cards.forEach(card => {
        if (!card.dataset.tiltInit) {
            // Só adiciona os eventos de rato se for um computador
            if (isDesktop) {
                card.addEventListener('mousemove', (e) => {
                    const rect = card.getBoundingClientRect();
                    const x = e.clientX - rect.left;
                    const y = e.clientY - rect.top;
                    const centerX = rect.width / 2;
                    const centerY = rect.height / 2;

                    const rotateX = ((y - centerY) / centerY) * -15;
                    const rotateY = ((x - centerX) / centerX) * 15;

                    card.style.transition = 'none';
                    card.style.transform = `perspective(1000px) scale(0.96) rotateX(${rotateX}deg) rotateY(${rotateY}deg)`;
                });

                card.addEventListener('mouseleave', () => {
                    card.style.transition = 'transform 0.5s ease';
                    card.style.transform = 'perspective(1000px) scale(1) rotateX(0deg) rotateY(0deg)';
                });
            }
            card.dataset.tiltInit = "true";
        }
    });

    // 3. DETEÇÃO DE PERCURSO EXATA (Intersection Observer)
    const wheel = document.getElementById('pathway-wheel');
    const items = document.querySelectorAll('.pathway-item');

    if (wheel && items.length > 0 && !wheel.dataset.observerInit) {
        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                // Ao baixar para 0.51, o item é reconhecido assim que entra na maioria da caixa
                if (entry.isIntersecting && entry.intersectionRatio >= 0.51) {
                    const newPathway = entry.target.getAttribute('data-pathway');
                    dotNetHelper.invokeMethodAsync('ChangePathwayFromJS', newPathway);
                }
            });
        }, {
            root: wheel,
            rootMargin: "-290px 0px -290px 0px",
            threshold: 0.51 // Mais tolerante para swipes rápidos no telemóvel
        });

        items.forEach(item => observer.observe(item));
        wheel.dataset.observerInit = "true";
    }
}

export function toggleBodyScroll(isLocked) {
    if (isLocked) {
        document.body.classList.add('modal-open');
    } else {
        document.body.classList.remove('modal-open');
    }
}

export function scrollToPathway(pathwayName) {
    // 1. Roda Desktop
    const desktopWheel = document.getElementById('pathway-wheel');
    if (desktopWheel && window.getComputedStyle(desktopWheel).display !== 'none') {
        const item = desktopWheel.querySelector(`[data-pathway="${pathwayName}"]`);
        if (item) item.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }

    // 2. Roda Mobile
    const mobileWheel = document.getElementById('mobile-wheel');
    if (mobileWheel && window.getComputedStyle(mobileWheel).display !== 'none') {
        const item = mobileWheel.querySelector(`[data-pathway="${pathwayName}"]`);
        // inline: 'center' alinha no meio do eixo horizontal
        if (item) item.scrollIntoView({ behavior: 'smooth', inline: 'center', block: 'nearest' });
    }
}

export function initMobileWheelObserver(dotNetHelper) {
    const wheel = document.getElementById('mobile-wheel');
    if (!wheel || wheel.dataset.observerInit) return;

    // Cria uma área de intersecção minúscula exatamente no meio do ecrã
    const options = {
        root: wheel,
        rootMargin: '0px -49% 0px -49%',
        threshold: 0
    };

    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                const pathway = entry.target.getAttribute('data-pathway');
                // Avisa o C# que um novo item parou no centro
                dotNetHelper.invokeMethodAsync('ChangePathwayFromJS', pathway);
            }
        });
    }, options);

    const items = wheel.querySelectorAll('.mobile-pathway-item');
    items.forEach(item => observer.observe(item));
    wheel.dataset.observerInit = "true";
}