<template>
  <div class="pacs-settings">
    <div class="settings-header">
      <h3>PACS Configuration</h3>
      <button @click="$emit('close')" class="close-btn">✕</button>
    </div>
    
    <div class="settings-content">
      <!-- Enable/Disable PACS -->
      <div class="setting-group">
        <label class="toggle-label">
          <input 
            type="checkbox" 
            v-model="localConfig.enabled"
            class="toggle-input"
          />
          <span class="toggle-switch"></span>
          <span>Enable PACS Upload</span>
        </label>
      </div>

      <!-- Connection Settings -->
      <div class="setting-group" :class="{ disabled: !localConfig.enabled }">
        <h4>Connection Settings</h4>
        
        <div class="form-row">
          <label>PACS Host:</label>
          <input 
            v-model="localConfig.host" 
            :disabled="!localConfig.enabled"
            placeholder="e.g., 192.168.1.100 or pacs.hospital.local"
            class="input"
          />
        </div>

        <div class="form-row">
          <label>Port:</label>
          <input 
            v-model.number="localConfig.port" 
            :disabled="!localConfig.enabled"
            type="number"
            min="1"
            max="65535"
            class="input small"
          />
        </div>

        <div class="form-row">
          <label>Called AE Title (PACS):</label>
          <input 
            v-model="localConfig.calledAETitle" 
            :disabled="!localConfig.enabled"
            placeholder="e.g., ORTHANC"
            maxlength="16"
            class="input"
            @input="validateAETitle('calledAETitle')"
          />
        </div>

        <div class="form-row">
          <label>Calling AE Title (Our):</label>
          <input 
            v-model="localConfig.callingAETitle" 
            :disabled="!localConfig.enabled"
            placeholder="e.g., SMARTBOX"
            maxlength="16"
            class="input"
            @input="validateAETitle('callingAETitle')"
          />
        </div>

        <div class="form-row">
          <label>Timeout (seconds):</label>
          <input 
            v-model.number="localConfig.timeout" 
            :disabled="!localConfig.enabled"
            type="number"
            min="5"
            max="300"
            class="input small"
          />
        </div>
      </div>

      <!-- Retry Settings -->
      <div class="setting-group" :class="{ disabled: !localConfig.enabled }">
        <h4>Retry Settings</h4>
        
        <div class="form-row">
          <label>Max Retries:</label>
          <input 
            v-model.number="localConfig.maxRetries" 
            :disabled="!localConfig.enabled"
            type="number"
            min="0"
            max="10"
            class="input small"
          />
        </div>

        <div class="form-row">
          <label>Retry Delay (seconds):</label>
          <input 
            v-model.number="localConfig.retryDelay" 
            :disabled="!localConfig.enabled"
            type="number"
            min="1"
            max="60"
            class="input small"
          />
        </div>
      </div>

      <!-- Test Connection -->
      <div class="setting-group" :class="{ disabled: !localConfig.enabled }">
        <button 
          @click="testConnection" 
          :disabled="!localConfig.enabled || testing"
          class="btn test-btn"
        >
          {{ testing ? 'Testing...' : 'Test Connection' }}
        </button>
        
        <div v-if="testResult" class="test-result" :class="testResult.success ? 'success' : 'error'">
          {{ testResult.message }}
        </div>
      </div>

      <!-- Action Buttons -->
      <div class="actions">
        <button @click="cancel" class="btn secondary">Cancel</button>
        <button @click="save" class="btn primary" :disabled="!isValid">Save</button>
      </div>
    </div>
  </div>
</template>

<script>
import { GetPACSConfig, SetPACSConfig, TestPACSConnection } from '../../wailsjs/go/main/App';

export default {
  name: 'PACSSettings',
  emits: ['close', 'saved'],
  
  data() {
    return {
      localConfig: {
        enabled: false,
        host: '',
        port: 104,
        calledAETitle: '',
        callingAETitle: 'SMARTBOX',
        timeout: 30,
        maxRetries: 3,
        retryDelay: 5,
        useTLS: false
      },
      testing: false,
      testResult: null,
      originalConfig: null
    };
  },
  
  computed: {
    isValid() {
      if (!this.localConfig.enabled) return true;
      
      return (
        this.localConfig.host.trim() !== '' &&
        this.localConfig.port > 0 &&
        this.localConfig.port <= 65535 &&
        this.isValidAETitle(this.localConfig.calledAETitle) &&
        this.isValidAETitle(this.localConfig.callingAETitle) &&
        this.localConfig.timeout >= 5 &&
        this.localConfig.maxRetries >= 0 &&
        this.localConfig.retryDelay >= 1
      );
    }
  },
  
  async mounted() {
    try {
      const config = await GetPACSConfig();
      this.localConfig = { ...config };
      this.originalConfig = { ...config };
    } catch (error) {
      console.error('Failed to load PACS config:', error);
    }
  },
  
  methods: {
    validateAETitle(field) {
      // AE Titles: uppercase, alphanumeric, spaces allowed, max 16 chars
      this.localConfig[field] = this.localConfig[field]
        .toUpperCase()
        .replace(/[^A-Z0-9 ]/g, '')
        .substring(0, 16);
    },
    
    isValidAETitle(title) {
      return title && /^[A-Z0-9 ]{1,16}$/.test(title);
    },
    
    async testConnection() {
      if (!this.isValid) return;
      
      this.testing = true;
      this.testResult = null;
      
      try {
        // Save config temporarily for test
        await SetPACSConfig(this.localConfig);
        
        // Test connection
        await TestPACSConnection();
        
        this.testResult = {
          success: true,
          message: 'Connection successful! PACS is reachable.'
        };
      } catch (error) {
        this.testResult = {
          success: false,
          message: error.message || 'Connection failed. Please check settings.'
        };
        
        // Restore original config if test failed
        if (this.originalConfig) {
          await SetPACSConfig(this.originalConfig);
        }
      } finally {
        this.testing = false;
      }
    },
    
    async save() {
      if (!this.isValid) return;
      
      try {
        await SetPACSConfig(this.localConfig);
        this.$emit('saved', this.localConfig);
        this.$emit('close');
      } catch (error) {
        console.error('Failed to save PACS config:', error);
        alert('Failed to save settings: ' + error.message);
      }
    },
    
    cancel() {
      this.$emit('close');
    }
  }
};
</script>

<style scoped>
.pacs-settings {
  background: white;
  border-radius: 8px;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.15);
  max-width: 600px;
  width: 100%;
  max-height: 90vh;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

.settings-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 20px;
  border-bottom: 1px solid #e0e0e0;
}

.settings-header h3 {
  margin: 0;
  color: #333;
}

.close-btn {
  background: none;
  border: none;
  font-size: 24px;
  cursor: pointer;
  color: #666;
  padding: 0;
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 4px;
  transition: all 0.2s;
}

.close-btn:hover {
  background: #f0f0f0;
  color: #333;
}

.settings-content {
  padding: 20px;
  overflow-y: auto;
  flex: 1;
}

.setting-group {
  margin-bottom: 24px;
  padding: 16px;
  background: #f8f9fa;
  border-radius: 8px;
  transition: opacity 0.3s;
}

.setting-group.disabled {
  opacity: 0.5;
}

.setting-group h4 {
  margin: 0 0 16px 0;
  color: #555;
  font-size: 16px;
}

.form-row {
  display: flex;
  align-items: center;
  margin-bottom: 12px;
}

.form-row label {
  flex: 0 0 180px;
  color: #666;
  font-size: 14px;
}

.input {
  flex: 1;
  padding: 8px 12px;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 14px;
  transition: border-color 0.3s;
}

.input:focus {
  outline: none;
  border-color: #007bff;
}

.input:disabled {
  background: #f0f0f0;
  cursor: not-allowed;
}

.input.small {
  flex: 0 0 100px;
}

/* Toggle Switch */
.toggle-label {
  display: flex;
  align-items: center;
  cursor: pointer;
  user-select: none;
}

.toggle-input {
  display: none;
}

.toggle-switch {
  position: relative;
  width: 48px;
  height: 24px;
  background: #ccc;
  border-radius: 12px;
  margin-right: 12px;
  transition: background 0.3s;
}

.toggle-switch::after {
  content: '';
  position: absolute;
  top: 2px;
  left: 2px;
  width: 20px;
  height: 20px;
  background: white;
  border-radius: 50%;
  transition: transform 0.3s;
}

.toggle-input:checked + .toggle-switch {
  background: #28a745;
}

.toggle-input:checked + .toggle-switch::after {
  transform: translateX(24px);
}

/* Buttons */
.btn {
  padding: 10px 20px;
  border: none;
  border-radius: 4px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.3s;
}

.btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.btn.primary {
  background: #007bff;
  color: white;
}

.btn.primary:hover:not(:disabled) {
  background: #0056b3;
}

.btn.secondary {
  background: #6c757d;
  color: white;
}

.btn.secondary:hover:not(:disabled) {
  background: #545b62;
}

.test-btn {
  background: #17a2b8;
  color: white;
  width: 100%;
}

.test-btn:hover:not(:disabled) {
  background: #138496;
}

.test-result {
  margin-top: 12px;
  padding: 12px;
  border-radius: 4px;
  font-size: 14px;
}

.test-result.success {
  background: #d4edda;
  color: #155724;
  border: 1px solid #c3e6cb;
}

.test-result.error {
  background: #f8d7da;
  color: #721c24;
  border: 1px solid #f5c6cb;
}

.actions {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  margin-top: 24px;
  padding-top: 20px;
  border-top: 1px solid #e0e0e0;
}
</style>