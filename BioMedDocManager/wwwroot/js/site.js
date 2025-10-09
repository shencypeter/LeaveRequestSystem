// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

$(document).ready(function () {
    $('#productClassDropdown').on('change', function () {
        const selectedProductClass = $(this).val();
        const $supplierSelect = $('#supplierName');
        const allOptions = $supplierSelect.find('option');
        const defaultOption = allOptions.filter('[value=""]');

        // Remove any previous fallback option
        $supplierSelect.find('option.fallback').remove();

        // Filter out default option and check for real matches
        const matchingOptions = allOptions.filter(function () {
            const productClass = $(this).data('product-class');
            return productClass === selectedProductClass;
        });

        if (selectedProductClass && matchingOptions.length > 0) {
            // Hide all, show only default + matching ones
            allOptions.hide();
            defaultOption.show();
            matchingOptions.show();
        } else {
            // Show all (fallback to original state)
            allOptions.show();

            if (selectedProductClass) {
                // Append fallback message as the top option
                const fallbackOption = $('<option>')
                    .addClass('fallback')
                    .text('該品項無符合的供應商，已顯示全部')
                    .prop('disabled', true)
                    .prop('selected', true);

                $supplierSelect.prepend(fallbackOption);
            }

        }

        // Clear the selected value unless it's the fallback
        $supplierSelect.val('');
    });
});

function CDocumentClaimAndReserve_ValidateForm() {
    const errors = [];

    const DateTime = document.querySelector("#DateTime")?.value;
    const docType = document.querySelector("input[name='rdbtype']:checked")?.value;

    /*
    選填
    const personName = document.querySelector("#txt_person_name")?.value?.trim();
    const projectName = document.querySelector("#txt_project_name")?.value?.trim();
    */
    // 共用欄位驗證
    if (!DateTime) {
        errors.push("請選擇領用日期。");
    }
    if (!docType) {
        errors.push("請選擇文件類別。");
    }

    // 類別 B：廠內文件
    if (docType === "B") {
        const bDocNo = document.querySelector("#txt_Boriginal_doc_no")?.value?.trim();
        const bDocVer = document.querySelector("#txt_Bdoc_ver")?.value?.trim();
        const bName = document.querySelector("#txt_Bname")?.value?.trim();
        const bPurpose = document.querySelector("#txt_Bpurpose")?.value?.trim();

        if (!bDocNo) {
            errors.push("請輸入表單編號。");
        }
        if (!bDocVer) {
            errors.push("請輸入表單版次。");
        }
        if (!bName) {
            errors.push("請輸入紀錄名稱。");
        }
        if (!bPurpose) {
            errors.push("請輸入領用目的。");
        }

        // 清除訊息
        $("#alert_msg").hide().text("");

        if (!validateClaimDateAfterIssueDate()) {
            errors.push("領用日期應大於等於該版次發行日期。");
            $("#alert_msg").show();
        }
    }

    // 類別 E：外來文件
    if (docType === "E") {
        // 原始編號非必填
        const eName = document.querySelector("#txt_Ename")?.value?.trim();
        const ePurpose = document.querySelector("#txt_Epurpose")?.value?.trim();

        if (!eName) {
            errors.push("請輸入文件名稱。");
        }
        if (!ePurpose) {
            errors.push("請輸入內容簡述。");
        }
    }

    if (errors.length > 0) {
        // 顯示錯誤訊息（用 SweetAlert2 或 alert）
        const htmlList = errors.map(e => e).join('');
        alert(htmlList);
        return false; // 阻止送出
    }

    return true; // 通過驗證

}

async function CDocumentClaimAndReserve_SubmitForm(form, submitUrl, submitBtn) {
    if (!CDocumentClaimAndReserve_ValidateForm()) return;

    const formData = new FormData(form);
    const token = getCSRFToken();

    // 防止重複提交
    submitBtn.prop("disabled", true).addClass("disabled");

    try {
        const response = await fetch(submitUrl, {
            method: "POST",
            body: formData,
            headers: {
                'RequestVerificationToken': token
            }
        });

        if (!response.ok) {
            const errData = await response.json();
            alert("領用失敗：" + (errData.errors?.join("") || "未知錯誤"));
            submitBtn.disabled = false;
            submitBtn.prop("disabled", false).removeClass("disabled");
            return;
        }

        const rdbtype = formData.get("rdbtype");

        if (rdbtype === "E") {
            // 外來文件
            alert(await response.text());
            // 成功後重新載入頁面
            setTimeout(() => {
                window.location.reload();
            }, 300);

            return;
        }

        // 下載邏輯
        const blob = await response.blob();

        // 嘗試從 Content-Disposition 中解析檔名
        const contentDisposition = response.headers.get("content-disposition");
        let filename = "領用文件.pdf";

        let match = contentDisposition?.match(/filename\*=UTF-8''([^;]+)/);
        if (match) {
            filename = decodeURIComponent(match[1]);
        } else {
            match = contentDisposition?.match(/filename="([^"]+)"/);
            if (match) {
                filename = match[1];
            }
        }

        // 觸發下載
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement("a");
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        a.remove();
        window.URL.revokeObjectURL(url);

        // 成功後重新載入頁面
        setTimeout(() => {
            window.location.reload();
        }, 300);

    } catch (err) {
        // console.error("Error during file download:", err);
        alert("領用失敗：" + err.message);
        submitBtn.prop("disabled", false).removeClass("disabled");
    }
}

function CDocumentClaimAndReserve_ChangerdBType(t) {
    $(".BDiv").toggle(t !== 'E');
    $(".EDiv").toggle(t === 'E');
}

// 貼上剪貼簿資料
async function handlePaste(text) {
    // console.log("text", text);

    if (!text) {
        alert('剪貼簿無內容!!');
        return;
    }

    const parts = text.split('\\');
    // console.log("parts", parts);

    // 對應順序：parts[0] = 表單編號, parts[1] = 版次, parts[2] = 表單名稱, parts[3] = 發行日期
    // 表單編號
    $('#txt_Boriginal_doc_no').val(safeVal(parts[0]));
    $('#txt_form_no').val(safeVal(parts[0]));

    // 版次
    $('#txt_Bdoc_ver').val(safeVal(parts[1]));
    $('#txt_form_version').val(safeVal(parts[1]));

    // 表單名稱
    $('#txt_Bname').val(safeVal(parts[2]));
    $('#txt_form_name').val(safeVal(parts[2]));

    // 發行日期處理：只保留 yyyy-MM-dd
    const issueDate = safeVal(parts[3]).split('T')[0] || '';
    $('#txt_BissueDatetime').val(issueDate);

    // console.log('已貼上!');
}

// 小工具：避免 undefined/null
function safeVal(val) {
    if (val === undefined || val === null) return '';
    if (typeof val === 'string' && val.trim().toLowerCase() === 'undefined') return '';
    return val;
}

// 檢查領用日期應大於發行日期
function validateClaimDateAfterIssueDate() {
    // 取得日期字串
    const issueDateStr = $("#txt_BissueDatetime").val();
    const claimDateStr = $("#DateTime").val();

    // 檢查都有填寫才進行比較
    if (issueDateStr && claimDateStr) {
        const issue = new Date(issueDateStr);
        const claim = new Date(claimDateStr);

        if (claim < issue) {
            return false;
        }
    }

    return true;
}


// 表格換頁
function navigatePage(page) {
    const url = new URL(window.location.href);
    url.searchParams.set("PageNumber", page);
    window.location.href = url.toString();
}

// 更新表格每頁顯示筆數
function updatePageSize() {
    //分頁 吃到 PageSize 控制參數
    const PageSize = document.getElementById("SelectPageSize").value;// 從<select>來的，所以不能單純寫PageSize
    const url = new URL(window.location.href);
    url.searchParams.set("PageSize", PageSize);
    url.searchParams.set("PageNumber", 1); // Reset to page 1 on size change

    // console.log(PageSize, url.toString());
    window.location.href = url.toString();
}

// url導向登入頁面
function goToLogin() {
    window.location.href = "/login";
}

// 關鍵字查詢 改成麵包屑名稱
function updateAccordionLabelFromTitle(title, itleSelector, accordionSelector) {

    const label = title + '查詢';

    $(accordionSelector).each(function () {
        const $btn = $(this);
        const $icon = $btn.find('i');
        $btn.contents().filter((i, el) => el.nodeType === 3).remove();
        $icon.after('&ensp;' + label);
    });
}

// 顯示訊息
function showModalMessage({ message, title = 'Notice', showCancel = false }) {
    return new Promise((resolve) => {
        const modalElement = document.getElementById('messageModal');
        const modalTitle = modalElement.querySelector('#messageModalLabel');
        const modalBody = modalElement.querySelector('#messageModalBody');
        const cancelButton = modalElement.querySelector('#messageModalCancel');
        const okButton = modalElement.querySelector('#messageModalOk');

        modalTitle.textContent = title;
        modalBody.innerHTML = message;

        cancelButton.style.display = showCancel ? 'inline-block' : 'none';

        const bsModal = new bootstrap.Modal(modalElement, {
            backdrop: 'static',
            keyboard: false
        });

        okButton.onclick = () => {
            bsModal.hide();
            resolve(true);
        };

        if (showCancel) {
            modalElement.addEventListener('hidden.bs.modal', () => {
                resolve(false);
            }, { once: true });
        } else {
            modalElement.addEventListener('hidden.bs.modal', () => {
                resolve(true);
            }, { once: true });
        }

        bsModal.show();
    });
}

// 顯示警告訊息
function showModalAlert(message, title = '警告') {
    return showModalMessage({ message, title, showCancel: false });
}

// 顯示成功訊息
function showModalSuccess(message, title = '成功') {
    return showModalMessage({ message, title, showCancel: false });
}

// 顯示確認訊息
function showModalConfirm(message, title = '確認') {
    return showModalMessage({ message, title, showCancel: true });
}




function setTableStyle() {
    const $thead = $("div.table table thead");

    // 表頭樣式用 class
    $thead.find("tr").addClass("table-header");

    // 排序箭頭包一層 span，加 class
    $thead.find("th").each(function () {
        const $th = $(this);
        let html = $th.html();

        if (html.includes("▲") || html.includes("▼")) {
            // 若尚未被 <span> 包住才包，避免重複
            html = html.replace(/(▲|▼)(?![^<]*<\/span>)/, '<span class="sort-arrow">$1</span>');
            $th.html(html);
        }
    });

    // 表頭連結改為 class（不要 .css）
    $thead.find("a").addClass("thead-link");
}

// 驗證確認密碼
function validateConfirmPassword(type) {
    var newPassword = $('#' + type + 'Password').val();
    var confirmPassword = $('#ConfirmPassword').val();

    if (newPassword !== confirmPassword) {
        $('#ConfirmPasswordValidation')
            .text('「密碼」與「確認密碼」不一致');
        return false;
    } else {
        $('#ConfirmPasswordValidation').text('');
        return true;
    }
}

function CheckConfirmPassword(type = "") {

    // 失去焦點時驗證
    $('#ConfirmPassword').on('blur', function () {
        validateConfirmPassword(type);
    });

    // 失去焦點時驗證
    $('#' + type + 'Password').on('blur', function () {
        validateConfirmPassword(type);
    });

    // 表單送出前驗證
    $('.PasswordForm').on('submit', function (e) {
        if (!validateConfirmPassword(type)) {
            e.preventDefault(); // 阻止提交
        }
    });
}

function tableSortListener(form_name) {
    $("th").click(function () {
        let column = $(this).data("id"); // Get column key from data-id attribute
        if (column == "RowNum" || column == "") {
            return;
        }

        $("#OrderBy").val(column);

        let currentOrder = $("#OrderBy").val();
        let currentDir = $("#SortDir").val();

        if (column === currentOrder || (column == "doc_ver" && currentOrder.includes("doc_ver"))) {
            // Toggle direction if the same column is clicked
            $("#SortDir").val(currentDir === "asc" ? "desc" : "asc");
        }
        else {
            $("#SortDir").val("asc");
        }



        // 只要是重新排序，就換回去第1頁
        $("#PageNumber").val(1);

        // console.log(currentDir, $("#SortDir").val());
        $("#" + form_name).submit(); // Submit form
    });
}

// Modal事件監聽
function modalListener(classSelector, mode = "normal") {
    const isDetail = mode === "detail";
    const iframeSelector = isDetail ? "modalDetailIframe" : "modalIframe";
    const modalSelector = isDetail ? "searchDetailModal" : "searchModal";
    const alertMessage = isDetail ? "No modal Detail URL specified." : "No modal URL specified.";

    $(document).on("click", classSelector, function (event) {
        event.preventDefault();

        const url = $(this).attr("href");
        if (!url) { alert(alertMessage); return; }

        // 設定 iframe 來源
        $("#" + iframeSelector).attr("src", url);

        // ★ 在開啟前綁一次 hook（避免 aria-hidden 警告）
        ensureModalA11yHook(document.getElementById(modalSelector));

        // 正常開啟
        openModal(modalSelector);
    });
}

// Modal停用焦點陷阱 + 把焦點移出 modal
function installModalA11yFix(targetDoc) {
    if (!targetDoc || targetDoc._modalA11yFixInstalled) return;

    targetDoc.addEventListener('hide.bs.modal', function (e) {
        const modalEl = e.target;
        // 1) 先停用 BS5 的焦點陷阱，避免它把焦點拉回 modal 裡
        try {
            const inst = modalEl.ownerDocument.defaultView.bootstrap?.Modal.getInstance(modalEl);
            inst?._focustrap?.deactivate?.(); // 私有屬性，存在才呼叫
        } catch (_) { }

        // 2) 把焦點移出 modal（到 body）
        const d = modalEl.ownerDocument;
        if (d.activeElement && modalEl.contains(d.activeElement)) {
            d.activeElement.blur();
            if (!d.body.hasAttribute('tabindex')) d.body.setAttribute('tabindex', '-1');
            d.body.focus({ preventScroll: true });
        }
    }, true); // ★ 用捕獲階段，早於屬性切換

    targetDoc._modalA11yFixInstalled = true;
}

// 只綁一次的無障礙補丁
function ensureModalA11yHook(modalEl) {
    if (!modalEl || modalEl._a11yHookApplied) return;
    modalEl.addEventListener('hide.bs.modal', function (e) {
        const d = e.target.ownerDocument;
        if (d.activeElement && e.target.contains(d.activeElement)) {
            d.activeElement.blur();
            if (!d.body.hasAttribute('tabindex')) d.body.setAttribute('tabindex', '-1');
            d.body.focus({ preventScroll: true });
        }
    }, /* capture */ true);
    modalEl._a11yHookApplied = true;
}

// 開啟Modal
function openModal(id = 'searchModal') {
    var modalEl = document.getElementById(id);
    var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
    modal.show();
}

// 關閉Modal
function closeModal(id = 'searchModal') {
    // 往上尋找擁有該 modal 的視窗（先本視窗，再 parent，一路到 top）
    let w = window, el = null;
    while (true) {
        try { el = w.document.getElementById(id); } catch (_) { el = null; }
        if (el) break;                                 // 找到了就用這個視窗
        if (!w.parent || w.parent === w) return;       // 到頂了還沒找到
        try { void w.parent.document; } catch (_) {     // 跨網域就停
            return;
        }
        w = w.parent;
    }

    // 關閉前把焦點移出 modal，避免 aria-hidden 警告
    try {
        const d = w.document;
        if (d.activeElement && el.contains(d.activeElement)) {
            d.activeElement.blur();
            (d.getElementById('mainContent') || d.body).focus?.();
        }
    } catch (_) { }

    // 用找到那個視窗自己的 bootstrap.Modal 來關
    if (!w.bootstrap || !w.bootstrap.Modal) return;
    const modal = w.bootstrap.Modal.getInstance(el) || w.bootstrap.Modal.getOrCreateInstance(el);
    modal.hide();
}

function dismiss(alertMsg = "") {
    // 嘗試關閉父畫面中的 Bootstrap Modal
    try {
        var modal = window.parent.document.querySelector('.modal.show');
        if (modal) {

            closeModal(modal.id);

            // console.log(alertMsg);
            if (alertMsg != "") {
                // 顯示訊息
                setTimeout(function () {
                    alert(alertMsg);
                }, 200);

                // 頁面重整
                setTimeout(function () {
                    window.parent.location.reload();
                }, 1200);
            }
            return;
        }
    } catch (e) {
        // console.log("dismiss Error：");
        // console.log(e);
    }
}

function dismissDetail() {
    try {
        const modalIframe = window.parent.document.getElementById("modalIframe");
        if (!modalIframe) return;

        const win1 = modalIframe.contentWindow;
        if (!win1) return;

        // 直接在第二層找 searchDetailModal
        const modalEl = win1.document.getElementById("searchDetailModal");
        // console.log("searchDetailModal in win1:", modalEl);
        if (!modalEl) return;

        const modal = win1.bootstrap.Modal.getInstance(modalEl)
            || new win1.bootstrap.Modal(modalEl);
        modal.hide();

    } catch (e) {
        // console.error("dismissDetail error:", e);
    }
}






// 查詢類型的按鈕，要重設分頁參數
function searchBtnListener(form) {
    $("#" + form).on("submit", function (e) {
        const submitter = e.originalEvent?.submitter;

        if ($(submitter).hasClass("search_btn")) {
            e.preventDefault();
            $("#PageNumber").val(1);
            $("#PageSize").val(10);
            this.submit();
        }
        // 否則不攔截，讓匯出 Excel 等其它行為正常進行
    });
}



// [一般頁面]請購編號查詢
function docSearchBtnListenerModal() {
    modalListener(".docSearch");
}

// [modal頁面]請購編號查詢
function docSearchBtnDetailModalListener() {
    modalListener(".docSearch", "detail");
}

// [一般頁面]文件編號查詢樹
function docTreeModalListener() {

    // 增加querystring
    $(document).on("click", '.doc_tree_search_btn', function (event) {
        const dateVal = $('#DateTime').val();

        if (dateVal) {
            const url = new URL(this.href, window.location.origin);
            url.searchParams.set('date', dateVal);
            this.href = url.toString(); // 重新設定 href
        }
    });

    modalListener(".doc_tree_search_btn");
}

// [modal頁面]文件編號查詢樹
function docTreeDetailModalListener() {
    // 增加querystring
    $(document).on("click", '.doc_tree_search_btn', function (event) {
        const dateVal = $('#DateTime').val();

        if (dateVal) {
            const url = new URL(this.href, window.location.origin);
            url.searchParams.set('date', dateVal);
            this.href = url.toString(); // 重新設定 href
        }
    });

    modalListener(".doc_tree_search_btn", "detail");
}

// [一般頁面]明細頁
function detailModalListener() {
    modalListener(".detail");
}

// [一般頁面]新增頁
function createModalListener() {
    modalListener("#create_btn");
}

// [一般頁面]更改密碼
function changePasswordModalListener() {
    modalListener(".change_password_btn");
}

// 重設iframe高度
function resizeIframe(height) {
    $('#iframeLoader').height(height);
}

function formClearInputListener() {
    $(document).on("click", ".clear_btn", function () {
        const formId = $(this).data("form");
        FormClearInput(formId);
    });
}

// 表格清除輸入資料
function FormClearInput(form) {
    $('#' + form)
        .find('input[type="radio"]:not([readonly]):not([disabled]), input[type="text"]:not([readonly]):not([disabled]), input[type="number"]:not([readonly]):not([disabled]), input[type="date"]:not([readonly]):not([disabled]),input[type="file"]:not([readonly]):not([disabled]), textarea:not([readonly]):not([disabled]), select:not([readonly]):not([disabled])')
        .val('')
        .prop('checked', false);

}

// 查詢表單樹貼上的事件監聽
function handlePasteListener() {
    window.addEventListener('message', async (event) => {
        if (event.data?.treeData) {
            // console.log('Received treeData via postMessage:', event.data.treeData);
            await handlePaste(event.data.treeData);
        }
    });
}



// 取得網址列QueryString
function getQueryStringValue(name) {
    const params = new URLSearchParams(window.location.search);
    return params.get(name);
}

// 表單查詢樹狀圖-建立樹狀圖
function buildTree(urlVersion, date, searchTerm = '') {
    const treeSelector = '#myTree';

    $(treeSelector).jstree('destroy'); // tear down any existing instance

    $(treeSelector).jstree({
        core: {
            data: {
                url: `/Tree/${urlVersion}?date=${date}&search=${encodeURIComponent(searchTerm)}`,
                dataType: 'json'
            },
            check_callback: true,
            themes: {
                dots: false,
                icons: true,
                responsive: false
            }
        },
        plugins: ['wholerow', 'contextmenu']
    }).on('ready.jstree open_node.jstree', function () {
        $(treeSelector)
            .find('ul.jstree-container-ul.jstree-children')
            .attr('class', 'jstree-contextmenu jstree-wholerow-ul jstree-no-dots');

        const tree = $(treeSelector).jstree(true);
        tree.get_json('#', { flat: true }).forEach(function (node) {
            if (node.original?.partialDocNo) {
                $(`#${node.id}_anchor`).attr('title', `${node.original.partialDocNo}`);
            }
        });
    }).on('ready.jstree', function () {
        const tree = $(treeSelector).jstree(true);

        // 💡 Expand all nodes when the tree is ready
        //tree.open_all();

        // Style fix
        $(treeSelector)
            .find('ul.jstree-container-ul.jstree-children')
            .attr('class', 'jstree-contextmenu jstree-wholerow-ul jstree-no-dots');

        // Tooltip setup
        tree.get_json('#', { flat: true }).forEach(function (node) {
            if (node.original?.partialDocNo) {
                $(`#${node.id}_anchor`).attr('title', `${node.original.partialDocNo}`);
            }
        });
    }).on('select_node.jstree', function (e, data) {
        // console.log("Selected node:", data.node);
    });

    $(treeSelector).off('dblclick.jstree').on('dblclick.jstree', function (e) {
        const node = $(treeSelector).jstree(true).get_node(e.target);
        if (node?.original?.partialDocNo) {
            sendTreeDataToParentAndClipboard(node.original.partialDocNo + "\\" + node.original.issueDatetime);

            if (confirm(`已複製 [${node.original.partialDocNo}]，是否關閉視窗?`)) {
                dismiss();
                dismissDetail();
            }
        }
    });
}


// 表單查詢樹狀圖-防抖函式：避免重複快速觸發（如即時搜尋時限制過度觸發）
function debounce(func, wait) {
    let timeout;
    return function () {
        const context = this, args = arguments;
        clearTimeout(timeout);
        timeout = setTimeout(() => func.apply(context, args), wait);
    };
}

// 表單查詢樹狀圖-將資料存到剪貼簿
function sendTreeDataToParentAndClipboard(data) {
    // Attempt clipboard write first
    navigator.clipboard.writeText(data)
        .then(() => {
            // console.log(`已複製到剪貼簿：${data}`);
        })
        .catch(() => {
            // console.warn('複製到剪貼簿失敗');
        });

    // Send via postMessage regardless
    if (window.parent && window.parent !== window) {
        //iframe
        window.parent.postMessage({ treeData: data }, '*');
    } else if (window.opener && !window.opener.closed) {
        //window open
        window.opener.postMessage({ treeData: data }, '*');
    } else {
        // console.warn('沒有收到任何剪貼簿資料');
    }
}


// 錯誤訊息頁面-倒數秒數自動回首頁、若是在Modal中，則關閉Modal
function startRedirectCountdown(seconds, redirectUrl) {
    let counter = seconds;
    const $seconds = $("#seconds");

    const timer = setInterval(function () {
        counter--;
        $seconds.text(counter);

        if (counter <= 0) {
            clearInterval(timer);

            // 如果是在主視窗中（非 iframe），執行導頁
            if (window.self === window.top) {
                window.location.href = redirectUrl;
            } else {
                // 否則關閉 modal（假設 parent 有定義 dismiss 方法）
                if (typeof window.parent.dismiss === "function") {
                    window.parent.dismiss();
                }
            }
        }
    }, 1000);
}


// 密碼重設頁-快捷鍵事件監聽
function copyUserNameButtonListener() {
    $(document).on("click", ".copyUserNameButton", function (event) {
        copyUserName();
    });
}


// 密碼重設頁-快捷鍵，重設密碼為Abcd+工號
function copyUserName() {
    var UserName = $("#UserName").val();
    $("#NewPassword").val("Abcd" + UserName);
    $("#ConfirmPassword").val("Abcd" + UserName);
}

// 密碼重設頁-密碼顯示按鈕事件監聽
function passwordVisibilityListener() {

    var stickyMode = false;

    if (stickyMode) {
        // 點一下切換模式
        $(document).on("click", ".togglePasswordButton", function (event) {
            var inputId = $(this).data("id");
            togglePasswordVisibility(inputId, this);
        });
    } else {
        // 按住顯示，放開隱藏
        $(document).on("mousedown", ".togglePasswordButton", function (event) {
            var inputId = $(this).data("id");
            $("#" + inputId).attr("type", "text");

            var icon = $(this).find("i")[0];
            if (icon) {
                icon.classList.remove("fa-eye");
                icon.classList.add("fa-eye-slash");
            }
        });

        $(document).on("mouseup mouseleave", ".togglePasswordButton", function (event) {
            var inputId = $(this).data("id");
            $("#" + inputId).attr("type", "password");

            var icon = $(this).find("i")[0];
            if (icon) {
                icon.classList.remove("fa-eye-slash");
                icon.classList.add("fa-eye");
            }
        });
    }
}

// 密碼重設頁-密碼顯示按鈕
function togglePasswordVisibility(inputId, btn) {
    const input = $("#" + inputId);
    const isPassword = input.attr("type") === "password";
    input.attr("type", isPassword ? "text" : "password");

    // 可選：切換圖示
    const icon = btn.querySelector("i");
    icon.classList.toggle("fa-eye");
    icon.classList.toggle("fa-eye-slash");
}

// 品項分類選擇後，篩選供應商
function filterSuppliersByProductClass(productClassValue, supplierSelector) {
    const $supplierSelect = $(supplierSelector);
    const allOptions = $supplierSelect.find('option');
    const defaultOption = allOptions.filter('[value=""]');

    // 移除 fallback 提示
    $supplierSelect.find('option.fallback').remove();

    // 對所有 option 做篩選（排除 defaultOption）
    const matchingOptions = allOptions.filter(function () {
        const productClass = $(this).data('product-class');
        return productClass === productClassValue;
    });

    // 清除當前選擇
    $supplierSelect.val('');

    if (productClassValue && matchingOptions.length > 0) {
        allOptions.hide();
        defaultOption.show();
        matchingOptions.show();
    } else if (productClassValue) {
        allOptions.hide(); // 隱藏所有 option（包含 default）

        // 顯示 fallback
        const fallbackOption = $('<option>')
            .addClass('fallback')
            .text('無符合之供應商')
            .prop('disabled', true)
            .prop('selected', true);

        $supplierSelect.prepend(fallbackOption);
    } else {
        // 沒選品項分類，顯示全部
        allOptions.show();
    }
}

// 請購編號開窗，篩選領用目的
function filterTableByKeyword(inputSelector, tableSelector) {
    var keyword = $(inputSelector).val().toLowerCase().trim();

    $(tableSelector).find('tbody tr').each(function () {
        var rowText = $(this).text().toLowerCase();
        $(this).toggle(rowText.indexOf(keyword) > -1);
    });
}

// 請購編號開窗，選中後，填入輸入框
function setParentInput(inputId, value) {
    // console.log("setParentInput");
    // Inside an iframe, to access elements in the parent frame
    parent.$('#' + inputId).val(value);

    closeCurrentModal()

}

// 關閉最接近的Modal物件
function closeCurrentModal() {
    const modalWin = findClosestModalWindow();

    // 找到最上層（最後一個）已顯示的 modal 元素
    const $list = modalWin.$ ? modalWin.$('.modal.show') : null;
    const el = $list && $list.length ? $list[$list.length - 1]
        : modalWin.document.querySelector('.modal.show');
    if (!el) return;

    // 關閉前把焦點移出 modal，避免 aria-hidden 警告
    try {
        const d = modalWin.document;
        if (d.activeElement && el.contains(d.activeElement)) {
            d.activeElement.blur?.();
            (d.getElementById('mainContent') || d.body).focus?.();
        }
    } catch (_) { }

    // --- Bootstrap 5：用原生 API 關閉 ---
    if (modalWin.bootstrap && modalWin.bootstrap.Modal) {
        const inst = modalWin.bootstrap.Modal.getInstance(el)
            || modalWin.bootstrap.Modal.getOrCreateInstance(el);
        inst.hide();
        return;
    }

    // --- 後備：若是 BS4（有 jQuery plugin） ---
    if (modalWin.$ && modalWin.$.fn && modalWin.$.fn.modal) {
        modalWin.$(el).modal('hide');
    }
}

function findClosestModalWindow() {
    let current = window;
    try {
        while (current !== current.parent) {
            if (current.parent.$) {
                const $modal = current.parent.$('.modal.show');
                if ($modal.length > 0) return current.parent;
            }
            current = current.parent;
        }
    } catch (e) {
        // 可能遇到跨網域，直接跳出
    }
    return window.top || window;
}

// 檢查上傳的檔案類型
function checkFileExtension(fileInputSelector, allowedExtensions) {
    $(fileInputSelector).on('change', function () {
        const file = this.files[0];
        if (!file) return;

        const fileName = file.name.toLowerCase();
        const isValid = allowedExtensions.some(ext => fileName.endsWith(ext));

        if (!isValid) {
            alert("僅允許上傳「" + allowedExtensions.join(" 或 ") + "」格式之檔案！");
            $(this).val(''); // 清空選擇
        }
    });
}

// 評核與其他紀錄-取得評分
function sanitizeInput($input) {
    // Get the min and max attributes from the input
    const min = parseFloat($input.attr('min')) || 0;
    const max = parseFloat($input.attr('max')) || 100; // Fallback max value if not set

    let val = parseFloat($input.val());

    if (isNaN(val)) {
        $input.val('');
        return 0;
    }

    if (val < min) {
        val = min;
        $input.val(min);
    } else if (val > max) {
        val = max;
        $input.val(max);
    }

    return val;
}

// 評核與其他紀錄-確認評分
function checkScore() {
    let allValid = true; // Assume all fields are valid unless proven otherwise

    // Check each select element for a valid value
    $('#PriceSelect, #SpecSelect, #DeliverySelect, #ServiceSelect, #QualitySelect, #AssessResult').each(function () {
        const $this = $(this);
        if ($this.val() === "" || $this.val() === null) {
            allValid = false;
            $this.css('border', '2px solid red'); // Highlight the invalid field
        } else {
            $this.css('border', ''); // Reset the border if it's valid
        }
    });

    if (!allValid) {
        $('#Grade').val(''); // Don't calculate the total if there are invalid inputs
        alert("請為所有必填項目評分");
        return false; // Stop execution if any field is invalid
    }

    return true;
}

// 評核與其他紀錄-顯示總分
function updateGradeTotal() {
    let total = 0;
    let allValid = true; // Assume all fields are valid unless proven otherwise

    // Check each select element for a valid value
    $('#PriceSelect, #SpecSelect, #DeliverySelect, #ServiceSelect, #QualitySelect').each(function () {
        const $this = $(this);
        if ($this.val() === "" || $this.val() === null) {
            allValid = false;
            $this.css('border', '2px solid red'); // Highlight the invalid field
        } else {
            $this.css('border', ''); // Reset the border if it's valid
        }
    });

    if (!allValid) {
        $('#Grade').val(''); // Don't calculate the total if there are invalid inputs
        // alert("請為所有必填項目評分");
        return; // Stop execution if any field is invalid
    }

    // Sanitize and sum the values
    total += sanitizeInput($('#PriceSelect'));
    total += sanitizeInput($('#SpecSelect'));
    total += sanitizeInput($('#DeliverySelect'));
    total += sanitizeInput($('#ServiceSelect'));
    total += sanitizeInput($('#QualitySelect'));

    $('#Grade').val(total);
}

// 品質協議、變更通知的required功能
function toggleRequired(selectId, inputSelector) {
    const selected = $(`#${selectId}`).val();
    const input = $(inputSelector);
    if (selected === "是") {
        input.prop('required', true);
    } else {
        input.prop('required', false);
    }
}

// 初供評核-評估項目的「其他」欄位處理
function toggleVisitOther() {
    const selected = $("#Visit").val();
    const otherInput = $("#VisitOther");

    if (selected === "其他") {
        otherInput.show().prop("required", true);
    } else {
        otherInput.hide().prop("required", false).val(""); // 清空內容避免送出
    }
}

// 初供評核-改善狀況顯示
function toggleImprovement() {
    if ($("#AssessResult").val() === "改善後合格") {
        $("#Improvement")
            .show()
            .attr("required", true); // 加上必填
        $("#ImprovementPlainText").hide();
    } else {
        $("#Improvement")
            .hide()
            .removeAttr("required") // 移除必填
            .val(""); // 清空內容
        $("#ImprovementPlainText").show();
    }
}

// 判斷日期A是否大於日期B(true：A大於B，false：A小於B)
function isDateAGreaterOrEqualThanB(dateA, dateB) {
    if (!dateA || !dateB) return false;

    const dA = new Date(dateA);
    const dB = new Date(dateB);

    // 無效日期處理
    if (isNaN(dA) || isNaN(dB)) return false;

    return dA >= dB;
}

// 驗證入庫日期是否 >= 比對目標日期(領用日期/表單發行日期)
function checkStockInTime(compareSelector, message) {
    const dateA = $('#InTime').val(); // 入庫日期
    const dateB = $(compareSelector).val(); // 比對日期

    if (dateA !== "") {
        if (!isDateAGreaterOrEqualThanB(dateA, dateB)) {
            $("#stockAlert").html(message);
            $('#stockAlert').show();
            $("#StockInSubmitBtn").prop("disabled", true);
            return false;
        } else {
            $("#stockAlert").html("");
            $('#stockAlert').hide();
            $("#StockInSubmitBtn").prop("disabled", false);
            return true;
        }
    } else {
        $("#stockAlert").html("");
        $('#stockAlert').hide();
        $("#StockInSubmitBtn").prop("disabled", false);
        return true;
    }
}

function getCSRFToken() {
    return $('input[name="__RequestVerificationToken"]').val();
}

// 抓文件編號(B/E，空/Reserve)
async function getDocumentClaimNumber(urlType = "") {
    const token = getCSRFToken();
    // 領用日期
    let selectedDate = $("#DateTime").val();

    let docType = $("#rdbtype_B").prop("checked") ? 'B' : 'E';

    try {
        const response = await fetch('/CDocumentClaim' + urlType + '/getDoumentClaimNumber', {
            method: 'POST',
            headers: {
                'RequestVerificationToken': token
            },
            body: new URLSearchParams({
                date: selectedDate,
                docType: docType
            })
        });

        if (!response.ok) {
            throw new Error('伺服器回應錯誤');
        }

        let result = await response.text();

        if (!result.startsWith("B") && !result.startsWith("E")) {
            // 錯誤
            alert(result);
        }

        $("." + docType + "Div input[name='txt_nextIdNo']").val(result);

    } catch (err) {
        // console.error(err);
        alert('取得文件編號失敗，請稍後再試。');
    }
}

// ajax載入供應商資訊
function loadQualifiedSuppliers(supplierName) {
    if (!supplierName) {
        showAlert("請先輸入或選擇供應商名稱", "warning", 4000);
        return;
    }

    const token = getCSRFToken();

    $.ajax({
        url: "/PSupplier1stAssess/LoadQualifiedSuppliers",
        type: "POST",
        data: { supplierName: supplierName },
        headers: {
            "RequestVerificationToken": token
        },
        dataType: "json",
        success: function (data) {

            $("#qualifiedSupplier_SupplierNo").val(data.supplierNo ?? "");
            $("#SupplierClass").val(data.supplierClass ?? "");
            $("#qualifiedSupplier_Tele").val(data.tele ?? "");
            //$("#ProductClass").val(data.productClass ?? "");
            $("#qualifiedSupplier_Tele2").val(data.tele2 ?? "");
            $("#qualifiedSupplier_Remarks").val(data.remarks ?? "");
            $("#qualifiedSupplier_Fax").val(data.fax ?? "");
            $("#qualifiedSupplier_Address").val(data.address ?? "");
            $("#qualifiedSupplier_SupplierInfo").val(data.supplierInfo ?? "");

            hideAlert();

        },
        error: function (xhr, status, err) {
            showAlert("警告：" + xhr.responseJSON.message + "。可於上方欄位輸入資料，以新增全新的供應商。", "warning");
        }
    });
}

// 可設定自動隱藏毫秒數 timeoutMs，0 = 不自動隱藏
function showAlert(message, type = "info", timeoutMs = 0) {
    const $box = $("#pageAlert");
    // 清除舊的樣式
    $box
        .removeClass("d-none alert-info alert-success alert-warning alert-danger")
        .addClass(`alert-${type}`);

    // 設定文字（避免 XSS 請用 text）
    $box.find(".alert-content").text(message);

    // 顯示
    // 加上 .show 以配合 .fade 動畫（Bootstrap 5）
    $box.addClass("show");

    // 自動隱藏
    if (timeoutMs > 0) {
        window.clearTimeout($box.data("hideTimer"));
        const t = window.setTimeout(() => hideAlert(), timeoutMs);
        $box.data("hideTimer", t);
    }
}

function hideAlert() {
    const $box = $("#pageAlert");
    $box.removeClass("show").addClass("d-none");
}


function DeleteConfirmEventListener() {
    $('#btn-delete').on('click', function (e) {
        if (!confirm('注意：是否確定刪除該筆資料?')) {
            e.preventDefault(); // 阻止送出
        }
    });
}

function batchInTimeConfirmEventListener() {
    $('#batch_in_time_btn').on('click', function (e) {
        if (!confirm('確認入庫?')) {
            e.preventDefault(); // 阻止送出
        }
    });
}

// 品項選單維護-啟用停用按鈕
function toggleDisabledText(inputSelector, btnSelector, disabledText, classEnabled, classDisabled) {
    const $input = $(inputSelector);
    const $btn = $(btnSelector);
    const $icon = $btn.find("i");

    $btn.on("click", function () {
        let val = $input.val().trim();

        if (val.includes(disabledText)) {
            // 移除 "停用"
            val = val.replace(disabledText, "").trim();
            $icon.attr("class", classEnabled).removeClass("text-danger");
        } else {
            // 加上 "停用"
            val = (val + " " + disabledText).trim();
            $icon.attr("class", classDisabled);
        }

        $input.val(val);
    });
}

// 刷新驗證碼
function refreshCaptcha(img) {
    const base = img.getAttribute('data-url');
    img.src = base + '?t=' + Date.now(); // 防快取
}