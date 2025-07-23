/**
 * Web Worker für Thumbnail-Generierung
 * Nutzt OffscreenCanvas für Performance
 */

let videoElement = null;
let canvas = null;
let ctx = null;

self.onmessage = async function(e) {
    const { command, ...data } = e.data;
    
    switch (command) {
        case 'generateThumbnails':
            await generateThumbnails(data);
            break;
        case 'generateSingleThumbnail':
            await generateSingleThumbnail(data);
            break;
        case 'clear':
            clearResources();
            break;
    }
};

async function generateThumbnails(data) {
    const { videoBlob, duration, interval, thumbnailWidth, fps } = data;
    
    try {
        // Create video element in worker
        const video = await createVideoElement(videoBlob);
        
        // Setup offscreen canvas
        const height = Math.floor(thumbnailWidth * 9 / 16); // 16:9 aspect ratio
        canvas = new OffscreenCanvas(thumbnailWidth, height);
        ctx = canvas.getContext('2d');
        
        const totalFrames = Math.floor(duration * fps);
        const thumbnailCount = Math.floor(totalFrames / interval);
        
        // Generate thumbnails at intervals
        for (let i = 0; i < thumbnailCount; i++) {
            const frameNumber = i * interval;
            const time = frameNumber / fps;
            
            // Seek to time
            video.currentTime = time;
            await waitForSeek(video);
            
            // Draw frame
            ctx.drawImage(video, 0, 0, thumbnailWidth, height);
            
            // Convert to blob
            const blob = await canvas.convertToBlob({
                type: 'image/jpeg',
                quality: 0.7
            });
            
            // Send back to main thread
            self.postMessage({
                frameNumber,
                thumbnail: blob,
                progress: ((i + 1) / thumbnailCount) * 100
            });
            
            // Small delay to prevent blocking
            if (i % 10 === 0) {
                await sleep(10);
            }
        }
        
        // Cleanup
        video.remove();
        
    } catch (error) {
        self.postMessage({
            error: error.message
        });
    }
}

async function generateSingleThumbnail(data) {
    const { videoBlob, frameNumber, thumbnailWidth, fps } = data;
    
    try {
        const video = await createVideoElement(videoBlob);
        const height = Math.floor(thumbnailWidth * 9 / 16);
        
        if (!canvas || canvas.width !== thumbnailWidth) {
            canvas = new OffscreenCanvas(thumbnailWidth, height);
            ctx = canvas.getContext('2d');
        }
        
        const time = frameNumber / fps;
        video.currentTime = time;
        await waitForSeek(video);
        
        ctx.drawImage(video, 0, 0, thumbnailWidth, height);
        
        const blob = await canvas.convertToBlob({
            type: 'image/jpeg',
            quality: 0.7
        });
        
        self.postMessage({
            frameNumber,
            thumbnail: blob
        });
        
        video.remove();
        
    } catch (error) {
        self.postMessage({
            error: error.message
        });
    }
}

async function createVideoElement(source) {
    return new Promise((resolve, reject) => {
        const video = document.createElement('video');
        video.crossOrigin = 'anonymous';
        video.muted = true;
        
        video.onloadedmetadata = () => resolve(video);
        video.onerror = reject;
        
        if (typeof source === 'string') {
            video.src = source;
        } else if (source instanceof Blob) {
            video.src = URL.createObjectURL(source);
        }
    });
}

async function waitForSeek(video) {
    return new Promise((resolve) => {
        if (video.seekable.length > 0) {
            video.onseeked = () => {
                video.onseeked = null;
                resolve();
            };
        } else {
            // Fallback for videos that don't support seeking
            setTimeout(resolve, 100);
        }
    });
}

function clearResources() {
    if (videoElement) {
        videoElement.remove();
        videoElement = null;
    }
    canvas = null;
    ctx = null;
}

function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

// Intelligent frame selection for better thumbnails
function selectKeyFrame(video, targetTime, range = 0.5) {
    // This would analyze frames around the target time
    // to find one with the most visual information
    // For now, just return the target time
    return targetTime;
}

// Scene detection helper
async function detectSceneChange(video, time1, time2, threshold = 30) {
    // Compare two frames to detect if there's a scene change
    // This would use pixel difference analysis
    // Placeholder for now
    return false;
}