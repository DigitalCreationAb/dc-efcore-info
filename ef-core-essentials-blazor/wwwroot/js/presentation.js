let _ref = null;
let _handler = null;

export function init(dotnetRef) {
    _ref = dotnetRef;
    _handler = (e) => {
        if (['ArrowRight', 'ArrowDown', ' '].includes(e.key)) {
            e.preventDefault();
            dotnetRef.invokeMethodAsync('HandleKey', 'next');
        } else if (['ArrowLeft', 'ArrowUp'].includes(e.key)) {
            e.preventDefault();
            dotnetRef.invokeMethodAsync('HandleKey', 'prev');
        }
    };
    document.addEventListener('keydown', _handler);
}

export function dispose() {
    if (_handler)
        document.removeEventListener('keydown', _handler);
}
