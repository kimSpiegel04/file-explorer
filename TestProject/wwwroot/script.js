let currentSortBy = 'size';
let currentSortDirection = 'asc';

document.getElementById("openDialogBtn").addEventListener("click", () => {
    const urlParams = new URLSearchParams(window.location.search);
    if (!urlParams.has("path")) {
        urlParams.set("path", "/");
    }
    urlParams.set("dialog", "true");
    history.pushState(null, "", `?${urlParams.toString()}`);
    syncFromURL();
    document.getElementById("fileExplorerDialog").style.display = "block";
    document.getElementById("openDialogBtn").style.display = "none";
});

document.getElementById("closeDialogBtn").addEventListener("click", () => {
    const urlParams = new URLSearchParams(window.location.search);
    urlParams.delete("dialog");
    history.pushState(null, "", `?${urlParams.toString()}`);
    syncFromURL();
    document.getElementById("fileExplorerDialog").style.display = "none";
    document.getElementById("openDialogBtn").style.display = "block";
});

document.addEventListener("keydown", (e) => {
    if (e.key === "Escape") {
        document.getElementById("fileExplorerDialog").style.display = "none";
        document.getElementById("openDialogBtn").style.display = "block";
    }
});

function syncFromURL() {
    const urlParams = new URLSearchParams(window.location.search);
    const path = urlParams.get("path");
    const page = parseInt(urlParams.get("page")) || 1;
    const sortBy = urlParams.get("sortBy");
    const sortDirection = urlParams.get("sortDirection");
    const dialogOpen = urlParams.get("dialog") === "true";

    if (sortBy) {
        currentSortBy = sortBy;
    }
    if (sortDirection) {
        currentSortDirection = sortDirection;
    }

    if (dialogOpen) {
        document.getElementById("fileExplorerDialog").style.display = "block";
        if (path) {
            document.getElementById("pathInput").value = path;
            loadFiles(path, page);
        }
        document.getElementById("openDialogBtn").style.display = "none";
    } else {
        document.getElementById("fileExplorerDialog").style.display = "none";
    }
}

window.addEventListener("DOMContentLoaded", syncFromURL);


function updateSort(column) {
    const path = document.getElementById("pathInput").value;
    if (currentSortBy === column) {
        currentSortDirection = currentSortDirection === "asc" ? "desc" : "asc";
    } else {
        currentSortBy = column;
        currentSortDirection = "asc";
    }
    // go back to page 1 when sorting
    loadFiles(path, 1); 
}

document.addEventListener('DOMContentLoaded', function() {
    var pathInput = document.getElementById("pathInput");
    var searchInput = document.getElementById("searchInput");
    var button = document.getElementById("pathButton");

    pathInput.addEventListener("keydown", function(event) {
        if (event.key === "Enter") {
            event.preventDefault(); 
            button.click(); 
        }
    });
    searchInput.addEventListener("keydown", function(event) {
        if (event.key === "Enter") {
            event.preventDefault(); 
            button.click(); 
        }
    });
});

document.getElementById("uploadFile").addEventListener("submit", async function (e) {
    e.preventDefault();

    const path = document.getElementById("pathInput").value;
    const fileInput = document.getElementById("fileInput");
    const file = fileInput.files[0];

    if (!file) {
        return;
    }

    const formData = new FormData();
    formData.append("file", file);

    try {
        const res = await fetch(`/api/files/upload?path=${encodeURIComponent(path)}`, {
            method: "POST",
            body: formData
        });

        const result = await res.json();
        document.getElementById("uploadStatus").innerText = `Upload of ${result.fileName} successful!`;
        loadFiles(path, 1); 
    } catch (err) {
        document.getElementById("uploadStatus").innerText = "Upload failed.";
    }
});

function formatBytes(bytes, decimals = 2) {
    if (bytes === 0) return '0 B';

    const k = 1024;
    const dm = decimals < 0 ? 0 : decimals;
    const sizes = ['B', 'KB', 'MB', 'GB', 'TB', 'PB'];

    let i = Math.floor(Math.log(bytes) / Math.log(k));

    return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i];
}

function getSortArrow(column) {
    if (currentSortBy !== column) {
        return "";
    }
    return currentSortDirection === "asc" ? " <span>↑</span>" : " <span>↓</span>";
}

function createFileTable(path, items) {
    const container = document.getElementById("results");
    const existingTable = document.getElementById("fileTable");
    if (existingTable) {
        existingTable.remove();
    }
    const table = document.createElement("table");
    table.id = "fileTable";
    let innerTable = "";

    innerTable += `
        <tr class='headerRow'>
            <th><a href="#" onclick="updateSort('name')">Name${getSortArrow('name')}</a></th>
            <th><a href="#" onclick="updateSort('size')">Size${getSortArrow('size')}</a></th>
            <th><a href="#" onclick="updateSort('date')">Date Modified${getSortArrow('date')}</a></th>
            <th>Actions</th>
        </tr>`;

    for (var i = 0; i < items.length; i++) {
        let name;
        if (items[i].type === "folder") {
            const newPath = path.endsWith('/') ? path + items[i].name : path + '/' + items[i].name;
            name = `📁<a class="folder" href="?path=${encodeURIComponent(newPath)}">${items[i].name}</a>`;
        } else {
            name = `<a href="/api/files/download?path=${encodeURIComponent(path + '/' + items[i].name)}" download>📄 ${items[i].name}</a>`;
        }
        let size = items[i].size ? formatBytes(items[i].size) : "";
        let lastModified = new Date(items[i].lastModified).toLocaleString();
        let deleteButton = `<button
                title = "Delete Item" 
                onclick = "deleteItem('${path + '/' + items[i].name}')">
                    🗑️
                </button>`;
        let moveButton = `<button
                title = "Move Item"
                onclick = "moveOrCopyItem('${path + '/' + items[i].name}', 'move')">
                    📁🏃‍♀️
                </button>`;
        let copyButton = `<button
                title = "Copy Item"
                onclick = "moveOrCopyItem('${path + '/' + items[i].name}', 'copy')">
                    📁➕
                </button>`;

        innerTable += `
            <tr class="data">
                <td>${name}</td>
                <td style="text-align: right">${size}</td>
                <td style="text-align: right">${lastModified}</td>
                <td>${deleteButton} ${moveButton} ${copyButton}</td>
            </tr>`;
    }
    table.innerHTML = innerTable;
    container.appendChild(table);
}

function renderBreadcrumb(path) {
    const breadcrumbDiv = document.createElement("div");
    breadcrumbDiv.style.marginBottom = "10px";

    const parts = path.split('/').filter(part => part !== '');
    let accumulatedPath = path.startsWith('/') ? '/' : '';

    const homeLink = document.createElement("a");
    const homeParams = new URLSearchParams(window.location.search);
    homeParams.set("path", "/");
    homeLink.href = `?${homeParams.toString()}`;
    homeLink.textContent = "/";
    breadcrumbDiv.appendChild(homeLink);
    
    for (let i = 0; i < parts.length; i++) {
        accumulatedPath += (accumulatedPath === '/' ? '' : '/') + parts[i];

        const separator = document.createTextNode(" / ");
        const spaceNode = document.createTextNode(' ');
        if (i > 0) {
            breadcrumbDiv.appendChild(separator);
        } else {
            breadcrumbDiv.appendChild(spaceNode);
        }
        
        if (i === parts.length - 1) {
            const span = document.createElement("span");
            span.textContent = parts[i];
            breadcrumbDiv.appendChild(span);
        } else {
            const link = document.createElement("a");
            const currentParams = new URLSearchParams(window.location.search);
            currentParams.set("path", accumulatedPath);
            link.href = `?${currentParams.toString()}`;
            link.textContent = parts[i];
            breadcrumbDiv.appendChild(link);
        }
    }
    if (path !== '/') {
        const separator = document.createTextNode(" / ");
        breadcrumbDiv.appendChild(separator);
    }

    breadcrumbDiv.querySelectorAll('a').forEach(link => {
        link.addEventListener('click', (e) => {
            e.preventDefault();
            const href = link.getAttribute('href');
            const urlParams = new URLSearchParams(href.split('?')[1]);
            const newPath = urlParams.get('path');
            loadFiles(newPath, 1);
            return false; 
        });
    });    

    return breadcrumbDiv;
}

async function loadFiles(path = document.getElementById("pathInput").value, page = 1) {
    const search = document.getElementById("searchInput").value;
    const urlParams = new URLSearchParams(window.location.search); 
    urlParams.set("path", path);
    urlParams.set("page", page);
    urlParams.set("sortBy", currentSortBy);
    urlParams.set("sortDirection", currentSortDirection);
    
    if (search) {
        urlParams.set("search", search);
    } else {
        urlParams.delete("search");
    }
    if (path != document.getElementById("pathInput").value) {
        document.getElementById("pathInput").value = path;
    }

    history.pushState(null, "", `?${urlParams.toString()}`);
    const container = document.getElementById("results");
    container.innerHTML = "Loading...";

    try {
        const res = await fetch(`/api/files?${urlParams.toString()}`);
        const data = await res.json();

        container.innerHTML = "";
        const breadcrumb = renderBreadcrumb(path); 
        container.appendChild(breadcrumb);

        if (!data.items || data.items.length === 0) {
            container.innerHTML += "<p>No files or folders found.</p>";
            return;
        }

        container.innerHTML += `<p>Item Count: ${data.folderCount + data.fileCount}</p>`;
        container.innerHTML += `<p>Total Size: ${formatBytes(data.totalSize)}</p>`;
        createFileTable(data.path, data.items);
        document.querySelectorAll('a.folder').forEach(link => {
            link.addEventListener('click', (e) => {
                e.preventDefault();
                const href = link.getAttribute('href');
                const urlParams = new URLSearchParams(href.split('?')[1]);
                const newPath = urlParams.get('path');
                loadFiles(newPath, 1); 
            });
        });
        renderPaginationControls(data.currentPage, data.totalPages);

    } catch (err) {
        console.log(err)
        container.innerHTML = `<p style="color:red;">Error loading files. Check console for details.</p>`;
    }
}

async function deleteItem(path) {
    const currentPath = document.getElementById("pathInput").value;
    if (!confirm("Are you sure you want to delete this item?")) {
        return;
    }

    const res = await fetch(`/api/files/delete?path=${encodeURIComponent(path)}`, {
        method: 'DELETE'
    });

    if (res.ok) {
        alert(`Deleted item!`);
        loadFiles(currentPath, undefined);
    } else {
        const err = await res.json();
        alert("Error: " + err.error );
    }
}

async function moveOrCopyItem(sourcePath, action) {
    const currentPath = document.getElementById("pathInput").value;
    const root = sourcePath.split('/').slice(0, -1).join('/') || '/'; 
    const res = await fetch(`/api/files/directories?root=${encodeURIComponent(root)}`);
    const dirs = await res.json();

    if (!res.ok || !dirs.length) {
        alert("No available directories");
        return;
    }

    const destName = prompt("Select destination:\n" +
        dirs.map((dir) => dir.name).join('\n'));
    const fullPath = dirs.find(({ name, fullPath }) => name === destName);

    const destinationPath = `${fullPath.fullPath}/${sourcePath.split('/').pop()}`;

    const actionRes = await fetch(`/api/files/action?sourcePath=${encodeURIComponent(sourcePath)}&destinationPath=${encodeURIComponent(destinationPath)}&action=${action}`, {
        method: 'POST'
    });

    if (actionRes.ok) {
        alert(`${action} successful.`);
        loadFiles(currentPath, undefined);
    } else {
        const err = await actionRes.json();
        alert("Error: " + err.error);
    }
}

function renderPaginationControls(currentPage, totalPages) {
    const container = document.getElementById("results");
    const paginationDiv = document.createElement("div");
    paginationDiv.className = "pagination";
    paginationDiv.style.marginTop = "20px";

    const createButton = (label, page, disabled = false) => {
        const btn = document.createElement("button");
        btn.textContent = label;
        btn.disabled = disabled;
        btn.style.margin = "0 4px";
        btn.addEventListener("click", () => loadFiles(undefined, page));
        return btn;
    };

    paginationDiv.appendChild(createButton("← Prev", currentPage - 1, currentPage === 1));

    // page numbers
    for (let i = 1; i <= totalPages; i++) {
        const btn = createButton(i, i, i === currentPage);
        if (i === currentPage) btn.style.fontWeight = "bold";
        paginationDiv.appendChild(btn);
    }

    paginationDiv.appendChild(createButton("Next →", currentPage + 1, currentPage === totalPages));

    container.appendChild(paginationDiv);
}

window.addEventListener("popstate", syncFromURL);