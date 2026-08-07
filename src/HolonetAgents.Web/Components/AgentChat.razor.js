// Keeps a scrollable container pinned to the newest content, unless the reader has
// deliberately scrolled up to read history.
export function createAutoScroller(container) {
    const nearBottomThreshold = 64;
    let pinned = true;

    const scrollToBottom = () => {
        container.scrollTop = container.scrollHeight;
    };

    const onScroll = () => {
        const distanceFromBottom = container.scrollHeight - container.scrollTop - container.clientHeight;
        pinned = distanceFromBottom <= nearBottomThreshold;
    };

    const onContentChanged = () => {
        if (pinned) {
            scrollToBottom();
        }
    };

    container.addEventListener('scroll', onScroll, { passive: true });

    // Catches new messages and text streamed into existing ones.
    const mutationObserver = new MutationObserver(onContentChanged);
    mutationObserver.observe(container, { childList: true, subtree: true, characterData: true });

    // Catches reflow from wrapping, images, or the container itself resizing.
    const resizeObserver = new ResizeObserver(onContentChanged);
    resizeObserver.observe(container);

    scrollToBottom();

    return {
        scrollToBottom,
        dispose: () => {
            container.removeEventListener('scroll', onScroll);
            mutationObserver.disconnect();
            resizeObserver.disconnect();
        }
    };
}
