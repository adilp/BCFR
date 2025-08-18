import { ReactNode } from 'react';
import './TabNavigation.css';

export interface Tab {
  id: string;
  label: string;
  icon?: ReactNode;
  badge?: string | number;
}

interface TabNavigationProps {
  tabs: Tab[];
  activeTab: string;
  onTabChange: (tabId: string) => void;
  variant?: 'default' | 'pills' | 'underline';
  size?: 'sm' | 'md' | 'lg';
}

function TabNavigation({ 
  tabs, 
  activeTab, 
  onTabChange, 
  variant = 'default',
  size = 'md' 
}: TabNavigationProps) {
  return (
    <div className={`tab-navigation tab-navigation-${variant} tab-navigation-${size}`}>
      {tabs.map((tab) => (
        <button
          key={tab.id}
          className={`tab-button ${activeTab === tab.id ? 'active' : ''}`}
          onClick={() => onTabChange(tab.id)}
        >
          {tab.icon && <span className="tab-icon">{tab.icon}</span>}
          <span className="tab-label">{tab.label}</span>
          {tab.badge !== undefined && (
            <span className="tab-badge">{tab.badge}</span>
          )}
        </button>
      ))}
    </div>
  );
}

export default TabNavigation;