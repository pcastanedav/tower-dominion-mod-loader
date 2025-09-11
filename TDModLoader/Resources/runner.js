let isResizing = false;
let currentPanel = null;

document.addEventListener('DOMContentLoaded', () => {
    // Button event listeners
    document.getElementById('runButton').addEventListener('click', runCode);
    document.getElementById('clearButton').addEventListener('click', clearCode);
    document.getElementById('formatButton').addEventListener('click', formatCode);
    document.getElementById('stopButton').addEventListener('click', stopExecution);
    document.getElementById('clearResultsButton').addEventListener('click', clearResults);
    document.getElementById('copyResultsButton').addEventListener('click', copyResults);
    document.getElementById('expandResultsButton').addEventListener('click', () => expandPanel('rightPanel'));

    // Code input event listeners
    const codeInput = document.getElementById('codeInput');
    codeInput.addEventListener('keydown', handleKeydown);
    codeInput.addEventListener('input', updateStats);

    // Resize functionality
    document.getElementById('leftResize').addEventListener('mousedown', startResize);
    document.addEventListener('mousemove', handleResize);
    document.addEventListener('mouseup', stopResize);

    updateStats();
});

function handleKeydown(e) {
    if (e.ctrlKey && e.key === 'Enter') {
        e.preventDefault();
        runCode();
    }
    if (e.key === 'Tab') {
        e.preventDefault();
        const start = e.target.selectionStart;
        const end = e.target.selectionEnd;
        e.target.value = e.target.value.substring(0, start) + '    ' + e.target.value.substring(end);
        e.target.selectionStart = e.target.selectionEnd = start + 4;
    }
}

function updateStats() {
    const code = document.getElementById('codeInput').value;
    const lines = code.split('\n').length;
    const chars = code.length;
    document.getElementById('lineCount').textContent = `Lines: ${lines}`;
    document.getElementById('charCount').textContent = `Characters: ${chars}`;
}

function togglePanel(panelId) {
    const panel = document.getElementById(panelId);
    const toggle = document.getElementById(panelId === 'leftPanel' ? 'leftToggle' : 'rightToggle');
    
    if (panel.classList.contains('collapsed')) {
        panel.classList.remove('collapsed');
        toggle.textContent = '▼';
    } else {
        panel.classList.add('collapsed');
        toggle.textContent = '▶';
    }
}

function expandPanel(panelId) {
    const panel = document.getElementById(panelId);
    const otherPanel = panelId === 'leftPanel' ? 
        document.getElementById('rightPanel') : 
        document.getElementById('leftPanel');
    
    if (panel.classList.contains('expanded')) {
        panel.classList.remove('expanded');
        otherPanel.classList.remove('collapsed');
    } else {
        panel.classList.add('expanded');
        otherPanel.classList.add('collapsed');
    }
}

function startResize(e) {
    isResizing = true;
    currentPanel = e.target.parentElement;
    document.body.style.cursor = 'col-resize';
    e.preventDefault();
}

function handleResize(e) {
    if (!isResizing) return;
    
    const container = document.querySelector('.container');
    const containerRect = container.getBoundingClientRect();
    const leftPanel = document.getElementById('leftPanel');
    
    const newWidth = ((e.clientX - containerRect.left) / containerRect.width) * 100;
    
    if (newWidth > 20 && newWidth < 80) {
        leftPanel.style.flex = `0 0 ${newWidth}%`;
        document.getElementById('rightPanel').style.flex = `0 0 ${100 - newWidth}%`;
    }
}

function stopResize() {
    isResizing = false;
    currentPanel = null;
    document.body.style.cursor = 'default';
}

async function runCode() {
    const code = document.getElementById('codeInput').value;
    const resultsDiv = document.getElementById('results');
    const statusSpan = document.getElementById('resultStatus');
    const timeSpan = document.getElementById('executionTime');
    const runBtn = document.getElementById('runButton');
    const stopBtn = document.getElementById('stopButton');

    if (!code.trim()) {
        resultsDiv.innerHTML = '⚠️ Please enter some C# code to execute.';
        resultsDiv.className = 'results error';
        statusSpan.textContent = 'Status: Error';
        return;
    }

    const startTime = Date.now();
    resultsDiv.innerHTML = '⏳ Executing code...';
    resultsDiv.className = 'results loading';
    statusSpan.textContent = 'Status: Running';
    runBtn.style.display = 'none';
    stopBtn.style.display = 'inline-flex';

    try {
        const response = await fetch('/code', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ code: code })
        });

        const result = await response.json();
        const executionTime = Date.now() - startTime;
        timeSpan.textContent = `Execution time: ${executionTime}ms`;

        if (result.success) {
            resultsDiv.innerHTML = `✅ Execution completed successfully!\n\nOutput:\n${result.result}`;
            resultsDiv.className = 'results success';
            statusSpan.textContent = 'Status: Success';
        } else {
            let errorMessage = `❌ Execution failed!\n\nError: ${result.error}`;
            if (result.details) {
                if (Array.isArray(result.details)) {
                    errorMessage += '\n\nDetails:\n' + result.details.join('\n');
                } else {
                    errorMessage += '\n\nDetails:\n' + result.details;
                }
            }
            resultsDiv.innerHTML = errorMessage;
            resultsDiv.className = 'results error';
            statusSpan.textContent = 'Status: Error';
        }
    } catch (error) {
        const executionTime = Date.now() - startTime;
        timeSpan.textContent = `Execution time: ${executionTime}ms`;
        resultsDiv.innerHTML = `🌐 Network Error: ${error.message}`;
        resultsDiv.className = 'results error';
        statusSpan.textContent = 'Status: Network Error';
    } finally {
        runBtn.style.display = 'inline-flex';
        stopBtn.style.display = 'none';
    }
}

function clearCode() {
    document.getElementById('codeInput').value = '';
    updateStats();
}

function formatCode() {
    // Basic C# formatting (this would need a proper formatter in a real implementation)
    const code = document.getElementById('codeInput').value;
    // This is a simple placeholder - real formatting would require a C# formatter
    alert('Code formatting feature would be implemented with a proper C# formatter library.');
}

function stopExecution() {
    // This would need to be implemented on the backend to actually stop execution
    alert('Stop execution feature would require backend implementation.');
}

function clearResults() {
    document.getElementById('results').innerHTML = 'Results cleared. Ready to execute C# code...';
    document.getElementById('results').className = 'results';
    document.getElementById('resultStatus').textContent = 'Status: Ready';
    document.getElementById('executionTime').textContent = 'Execution time: --';
}

async function copyResults() {
    const results = document.getElementById('results').textContent;
    try {
        await navigator.clipboard.writeText(results);
        const originalText = document.getElementById('copyResultsButton').innerHTML;
        document.getElementById('copyResultsButton').innerHTML = '✅ Copied!';
        setTimeout(() => {
            document.getElementById('copyResultsButton').innerHTML = originalText;
        }, 2000);
    } catch (err) {
        alert('Failed to copy to clipboard');
    }
}