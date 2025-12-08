/**
 * Admin Menu State Persistence - FIXED VERSION
 * Giữ trạng thái menu khi chuyển trang
 */

$(document).ready(function () {

    // === LẤY CONTROLLER VÀ ACTION HIỆN TẠI ===
    function getCurrentRoute() {
        const path = window.location.pathname;
        const parts = path.split('/').filter(p => p);

        // Path format: /Admin/Controller/Action
        return {
            area: parts[0] || '',
            controller: parts[1] || '',
            action: parts[2] || 'Index',
            fullPath: path.toLowerCase()
        };
    }

    // === 1. LƯU TRẠNG THÁI MENU ===
    function saveMenuState() {
        const openMenus = [];

        $('.nav-item.menu-open').each(function () {
            const menuText = $(this).find('> .nav-link p').first().text().trim();
            if (menuText) {
                openMenus.push(menuText);
            }
        });

        localStorage.setItem('adminMenuState', JSON.stringify(openMenus));
        console.log('✅ Saved menu state:', openMenus);
    }

    // === 2. KHÔI PHỤC TRẠNG THÁI MENU ===
    function restoreMenuState() {
        const savedState = localStorage.getItem('adminMenuState');

        if (savedState) {
            const openMenus = JSON.parse(savedState);
            console.log('🔄 Restoring menu state:', openMenus);

            openMenus.forEach(function (menuText) {
                $('.nav-item').each(function () {
                    const $item = $(this);
                    const currentText = $item.find('> .nav-link p').first().text().trim();

                    if (currentText === menuText) {
                        $item.addClass('menu-open');
                        $item.find('> .nav-treeview').show();
                    }
                });
            });
        }
    }

    // === 3. HIGHLIGHT MENU ĐÚNG - LOGIC MỚI ===
    function setActiveMenu() {
        const route = getCurrentRoute();
        console.log('📍 Current route:', route);

        // XÓA TẤT CẢ ACTIVE
        $('.nav-link').removeClass('active');

        let matchFound = false;

        // Duyệt qua TẤT CẢ các link
        $('.nav-link').each(function () {
            const $link = $(this);
            const href = $link.attr('href');

            // Skip link # (menu cha)
            if (!href || href === '#') {
                return;
            }

            const linkLower = href.toLowerCase();
            const pathLower = route.fullPath;

            // So sánh chính xác
            if (linkLower === pathLower) {
                $link.addClass('active');

                // Nếu là menu con, mở menu cha VÀ ACTIVE MENU CHA
                const $treeview = $link.closest('.nav-treeview');
                if ($treeview.length) {
                    const $parentItem = $treeview.parent('.nav-item');
                    $parentItem.addClass('menu-open');

                    // QUAN TRỌNG: Active menu cha (màu xanh)
                    $parentItem.find('> .nav-link').first().addClass('active');

                    $treeview.show();
                }

                matchFound = true;
                console.log('✅ Active menu:', $link.find('p').text().trim());
                return false; // Break loop
            }
        });

        if (!matchFound) {
            console.log('⚠️ No exact match found, trying partial match...');

            // Nếu không tìm thấy exact match, thử partial match
            $('.nav-link').each(function () {
                const $link = $(this);
                const href = $link.attr('href');

                if (!href || href === '#') return;

                const linkLower = href.toLowerCase();
                const pathLower = route.fullPath;

                // Check nếu path chứa controller và action
                if (pathLower.includes(route.controller.toLowerCase()) &&
                    linkLower.includes(route.controller.toLowerCase())) {

                    $link.addClass('active');

                    const $treeview = $link.closest('.nav-treeview');
                    if ($treeview.length) {
                        const $parentItem = $treeview.parent('.nav-item');
                        $parentItem.addClass('menu-open');

                        // QUAN TRỌNG: Active menu cha (màu xanh)
                        $parentItem.find('> .nav-link').first().addClass('active');

                        $treeview.show();
                    }

                    console.log('✅ Partial match:', $link.find('p').text().trim());
                    return false;
                }
            });
        }
    }

    // === 4. SỰ KIỆN: Click menu cha ===
    $('.nav-item > .nav-link').on('click', function (e) {
        const $parentItem = $(this).parent('.nav-item');
        const hasTreeview = $parentItem.find('.nav-treeview').length > 0;

        if (hasTreeview) {
            setTimeout(saveMenuState, 350);
        }
    });

    // === 5. SỰ KIỆN: Click menu con ===
    $('.nav-treeview .nav-link').on('click', function (e) {
        saveMenuState();
    });

    // === 6. SỰ KIỆN: Toggle sidebar ===
    $('[data-widget="pushmenu"]').on('click', function () {
        setTimeout(saveMenuState, 300);
    });

    // === 7. KHỞI CHẠY ===
    console.log('🚀 Initializing menu state...');

    // Delay nhỏ để đảm bảo DOM đã load xong
    setTimeout(function () {
        restoreMenuState();
        setActiveMenu();
    }, 100);
});

// === 8. XÓA TRẠNG THÁI KHI ĐĂNG XUẤT ===
$(document).on('click', 'a[href*="Logout"]', function () {
    localStorage.removeItem('adminMenuState');
    console.log('🔓 Menu state cleared on logout');
});