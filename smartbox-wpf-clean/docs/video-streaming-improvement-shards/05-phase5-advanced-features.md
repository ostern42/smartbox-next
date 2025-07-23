# Phase 5: Advanced Features (Priority: Low)

This phase adds collaborative features to enable multi-user interaction, shared annotations, and synchronized playback for medical team collaboration.

## 5.1 Collaborative Features

**File**: `collaborative-features.js` (New)

### Multi-User Collaboration System

```javascript
class CollaborativeFeatures {
    constructor(player, websocket) {
        this.player = player;
        this.ws = websocket;
        this.users = new Map();
        this.localUser = {
            id: this.generateUserId(),
            name: 'User',
            color: this.generateUserColor()
        };
        
        this.setupCollaboration();
    }
    
    setupCollaboration() {
        // Listen for remote events
        this.ws.addEventListener('message', (event) => {
            const message = JSON.parse(event.data);
            if (message.type.startsWith('collab:')) {
                this.handleCollaborativeMessage(message);
            }
        });
        
        // Announce presence
        this.announcePresence();
        
        // Set up heartbeat
        this.heartbeatInterval = setInterval(() => {
            this.sendHeartbeat();
        }, 30000);
    }
    
    handleCollaborativeMessage(message) {
        const { userId, data } = message;
        
        switch (message.type) {
            case 'collab:userJoined':
                this.addUser(data.user);
                break;
                
            case 'collab:userLeft':
                this.removeUser(userId);
                break;
                
            case 'collab:marker':
                this.showRemoteMarker(userId, data.marker);
                break;
                
            case 'collab:annotation':
                this.showRemoteAnnotation(userId, data.annotation);
                break;
                
            case 'collab:playbackSync':
                this.handlePlaybackSync(userId, data);
                break;
                
            case 'collab:cursor':
                this.updateRemoteCursor(userId, data.position);
                break;
        }
    }
}
```

### User Management

```javascript
    announcePresence() {
        this.broadcast('collab:userJoined', {
            user: this.localUser
        });
    }
    
    addUser(user) {
        this.users.set(user.id, user);
        this.player.emit('userJoined', user);
        
        // Show user indicator
        this.showUserIndicator(user);
    }
    
    removeUser(userId) {
        const user = this.users.get(userId);
        if (user) {
            this.users.delete(userId);
            this.player.emit('userLeft', user);
            
            // Remove user indicator
            this.hideUserIndicator(userId);
        }
    }
    
    showUserIndicator(user) {
        const indicator = document.createElement('div');
        indicator.className = 'user-indicator';
        indicator.id = `user-${user.id}`;
        indicator.style.backgroundColor = user.color;
        indicator.title = user.name;
        
        const avatar = document.createElement('div');
        avatar.className = 'user-avatar';
        avatar.textContent = user.name.charAt(0).toUpperCase();
        
        indicator.appendChild(avatar);
        this.player.container.querySelector('.users-list').appendChild(indicator);
    }
    
    hideUserIndicator(userId) {
        const indicator = document.getElementById(`user-${userId}`);
        if (indicator) {
            indicator.remove();
        }
    }
```

### Marker Collaboration

```javascript
    // Marker collaboration
    addMarker(timestamp, type, description) {
        const marker = {
            id: this.generateId(),
            timestamp: timestamp,
            type: type,
            description: description,
            userId: this.localUser.id,
            userColor: this.localUser.color,
            createdAt: Date.now()
        };
        
        // Add locally
        this.player.timeline.addMarker(marker);
        
        // Broadcast to others
        this.broadcast('collab:marker', { marker });
        
        return marker;
    }
    
    showRemoteMarker(userId, marker) {
        const user = this.users.get(userId);
        if (user) {
            // Add marker with user info
            marker.userName = user.name;
            marker.userColor = user.color;
            marker.isRemote = true;
            
            this.player.timeline.addMarker(marker);
        }
    }
```

### Annotation Collaboration

```javascript
    // Annotation collaboration
    addAnnotation(timestamp, text, position) {
        const annotation = {
            id: this.generateId(),
            timestamp: timestamp,
            text: text,
            position: position,
            userId: this.localUser.id,
            userColor: this.localUser.color,
            createdAt: Date.now()
        };
        
        // Add locally
        this.player.annotationLayer.addAnnotation(annotation);
        
        // Broadcast to others
        this.broadcast('collab:annotation', { annotation });
        
        return annotation;
    }
    
    showRemoteAnnotation(userId, annotation) {
        const user = this.users.get(userId);
        if (user) {
            annotation.userName = user.name;
            annotation.userColor = user.color;
            annotation.isRemote = true;
            
            this.player.annotationLayer.addAnnotation(annotation);
        }
    }
```

### Synchronized Playback

```javascript
    // Synchronized playback
    enableSyncedPlayback(masterId) {
        this.syncMaster = masterId;
        this.syncEnabled = true;
        
        if (masterId === this.localUser.id) {
            // As master, broadcast playback state
            this.player.on('play', () => this.broadcastPlaybackState());
            this.player.on('pause', () => this.broadcastPlaybackState());
            this.player.on('seek', () => this.broadcastPlaybackState());
        }
    }
    
    broadcastPlaybackState() {
        if (this.syncEnabled && this.syncMaster === this.localUser.id) {
            this.broadcast('collab:playbackSync', {
                playing: !this.player.video.paused,
                currentTime: this.player.video.currentTime,
                playbackRate: this.player.video.playbackRate
            });
        }
    }
    
    handlePlaybackSync(userId, data) {
        if (this.syncEnabled && userId === this.syncMaster) {
            // Sync to master's state
            this.player.seek(data.currentTime);
            this.player.video.playbackRate = data.playbackRate;
            
            if (data.playing && this.player.video.paused) {
                this.player.play();
            } else if (!data.playing && !this.player.video.paused) {
                this.player.pause();
            }
        }
    }
```

### Cursor Tracking

```javascript
    // Cursor tracking for timeline
    trackCursor(enabled = true) {
        if (enabled) {
            this.player.timeline.container.addEventListener('mousemove', 
                this.onCursorMove.bind(this));
        } else {
            this.player.timeline.container.removeEventListener('mousemove', 
                this.onCursorMove.bind(this));
        }
    }
    
    onCursorMove(event) {
        const rect = this.player.timeline.container.getBoundingClientRect();
        const position = {
            x: (event.clientX - rect.left) / rect.width,
            y: (event.clientY - rect.top) / rect.height,
            timestamp: this.player.timeline.pixelsToTime(event.clientX - rect.left)
        };
        
        this.broadcast('collab:cursor', { position });
    }
    
    updateRemoteCursor(userId, position) {
        const user = this.users.get(userId);
        if (!user) return;
        
        let cursor = document.getElementById(`cursor-${userId}`);
        if (!cursor) {
            cursor = document.createElement('div');
            cursor.className = 'remote-cursor';
            cursor.id = `cursor-${userId}`;
            cursor.style.backgroundColor = user.color;
            
            const label = document.createElement('span');
            label.className = 'cursor-label';
            label.textContent = user.name;
            cursor.appendChild(label);
            
            this.player.timeline.container.appendChild(cursor);
        }
        
        const rect = this.player.timeline.container.getBoundingClientRect();
        cursor.style.left = `${position.x * rect.width}px`;
        cursor.style.top = `${position.y * rect.height}px`;
    }
```

### Helper Methods

```javascript
    // Helper methods
    broadcast(type, data) {
        this.ws.send(JSON.stringify({
            type: type,
            userId: this.localUser.id,
            data: data
        }));
    }
    
    sendHeartbeat() {
        this.broadcast('collab:heartbeat', {
            timestamp: Date.now()
        });
    }
    
    generateUserId() {
        return `user_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
    }
    
    generateUserColor() {
        const colors = [
            '#FF6B6B', '#4ECDC4', '#45B7D1', '#F9CA24',
            '#6C5CE7', '#A29BFE', '#FD79A8', '#FDCB6E'
        ];
        return colors[Math.floor(Math.random() * colors.length)];
    }
    
    generateId() {
        return `${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
    }
    
    destroy() {
        if (this.heartbeatInterval) {
            clearInterval(this.heartbeatInterval);
        }
        
        // Announce departure
        this.broadcast('collab:userLeft', {});
    }
```

## UI Components

### User List Component

```javascript
class UsersList {
    constructor(container) {
        this.container = container;
        this.element = document.createElement('div');
        this.element.className = 'users-list';
        this.container.appendChild(this.element);
    }
    
    updateUsers(users) {
        this.element.innerHTML = '';
        users.forEach(user => {
            const userEl = document.createElement('div');
            userEl.className = 'user-item';
            userEl.innerHTML = `
                <div class="user-avatar" style="background-color: ${user.color}">
                    ${user.name.charAt(0).toUpperCase()}
                </div>
                <span class="user-name">${user.name}</span>
            `;
            this.element.appendChild(userEl);
        });
    }
}
```

### Sync Control Component

```javascript
class SyncControl {
    constructor(container, collaborative) {
        this.container = container;
        this.collaborative = collaborative;
        this.element = this.createElement();
        this.container.appendChild(this.element);
    }
    
    createElement() {
        const control = document.createElement('div');
        control.className = 'sync-control';
        
        const button = document.createElement('button');
        button.className = 'sync-button';
        button.textContent = 'Enable Sync';
        button.addEventListener('click', () => this.toggleSync());
        
        const status = document.createElement('div');
        status.className = 'sync-status';
        status.textContent = 'Sync: Off';
        
        control.appendChild(button);
        control.appendChild(status);
        
        return control;
    }
    
    toggleSync() {
        if (this.collaborative.syncEnabled) {
            this.collaborative.syncEnabled = false;
            this.updateStatus('Off');
        } else {
            // For demo, make local user the master
            this.collaborative.enableSyncedPlayback(this.collaborative.localUser.id);
            this.updateStatus('Master');
        }
    }
    
    updateStatus(status) {
        const statusEl = this.element.querySelector('.sync-status');
        statusEl.textContent = `Sync: ${status}`;
    }
}
```

## CSS Styles

```css
/* User indicators */
.users-list {
    position: absolute;
    top: 10px;
    right: 10px;
    display: flex;
    gap: 5px;
}

.user-indicator {
    width: 32px;
    height: 32px;
    border-radius: 50%;
    display: flex;
    align-items: center;
    justify-content: center;
    box-shadow: 0 2px 4px rgba(0,0,0,0.2);
}

.user-avatar {
    width: 28px;
    height: 28px;
    border-radius: 50%;
    display: flex;
    align-items: center;
    justify-content: center;
    color: white;
    font-weight: bold;
    font-size: 14px;
}

/* Remote cursors */
.remote-cursor {
    position: absolute;
    width: 20px;
    height: 20px;
    border-radius: 50%;
    pointer-events: none;
    transform: translate(-50%, -50%);
    transition: all 0.1s ease-out;
}

.cursor-label {
    position: absolute;
    top: -20px;
    left: 50%;
    transform: translateX(-50%);
    background: rgba(0,0,0,0.8);
    color: white;
    padding: 2px 6px;
    border-radius: 3px;
    font-size: 11px;
    white-space: nowrap;
}

/* Collaborative markers */
.timeline-marker.remote {
    opacity: 0.8;
}

.timeline-marker .user-info {
    position: absolute;
    bottom: 100%;
    left: 50%;
    transform: translateX(-50%);
    background: rgba(0,0,0,0.8);
    color: white;
    padding: 4px 8px;
    border-radius: 3px;
    font-size: 11px;
    margin-bottom: 5px;
    opacity: 0;
    transition: opacity 0.2s;
}

.timeline-marker:hover .user-info {
    opacity: 1;
}

/* Sync control */
.sync-control {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 5px;
}

.sync-button {
    padding: 5px 15px;
    background: #007bff;
    color: white;
    border: none;
    border-radius: 3px;
    cursor: pointer;
}

.sync-button:hover {
    background: #0056b3;
}

.sync-status {
    font-size: 12px;
    color: #666;
}
```

## Key Features

### Multi-User Support
- User presence indicators
- User-specific colors
- Heartbeat monitoring
- Automatic cleanup on disconnect

### Shared Annotations
- Markers with user attribution
- Text annotations
- Visual differentiation for remote content
- Hover information

### Synchronized Playback
- Master/follower model
- State synchronization
- Playback control sync
- Speed sync

### Real-time Cursors
- Timeline cursor tracking
- User identification
- Smooth animations

## Navigation

- [← Phase 4: Error Recovery](04-phase4-error-recovery.md)
- [Implementation & Testing →](06-implementation-testing.md)