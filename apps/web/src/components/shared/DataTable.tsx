import type { ReactNode } from 'react';
import { ChevronUpIcon, ChevronDownIcon } from '@heroicons/react/24/outline';
import './DataTable.css';

export interface Column<T> {
  key: string;
  label: string;
  accessor?: (item: T) => ReactNode;
  sortable?: boolean;
  width?: string;
  align?: 'left' | 'center' | 'right';
}

interface DataTableProps<T> {
  columns: Column<T>[];
  data: T[];
  keyExtractor: (item: T) => string;
  onRowClick?: (item: T) => void;
  expandable?: {
    renderExpanded: (item: T) => ReactNode;
    isExpanded: (item: T) => boolean;
    onToggle: (item: T) => void;
  };
  actions?: (item: T) => ReactNode;
  emptyMessage?: string;
  loading?: boolean;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
  onSort?: (column: string) => void;
  selectable?: {
    selected: Set<string>;
    onSelect: (id: string) => void;
    onSelectAll: () => void;
  };
}

function DataTable<T>({
  columns,
  data,
  keyExtractor,
  onRowClick,
  expandable,
  actions,
  emptyMessage = 'No data available',
  loading = false,
  sortBy,
  sortOrder,
  onSort,
  selectable
}: DataTableProps<T>) {
  const handleSort = (column: Column<T>) => {
    if (column.sortable && onSort) {
      onSort(column.key);
    }
  };

  if (loading) {
    return (
      <div className="data-table-loading">
        <div className="spinner" />
        <p>Loading...</p>
      </div>
    );
  }

  if (data.length === 0) {
    return (
      <div className="data-table-empty">
        <p>{emptyMessage}</p>
      </div>
    );
  }

  return (
    <div className="data-table-container">
      <table className="data-table">
        <thead>
          <tr>
            {selectable && (
              <th className="data-table-checkbox-cell">
                <input
                  type="checkbox"
                  checked={selectable.selected.size === data.length && data.length > 0}
                  onChange={() => selectable.onSelectAll()}
                />
              </th>
            )}
            {expandable && <th className="data-table-expand-cell" />}
            {columns.map((column) => (
              <th
                key={column.key}
                className={`data-table-header ${column.sortable ? 'sortable' : ''}`}
                style={{ width: column.width, textAlign: column.align || 'left' }}
                onClick={() => handleSort(column)}
              >
                <div className="data-table-header-content">
                  <span>{column.label}</span>
                  {column.sortable && sortBy === column.key && (
                    <span className="sort-indicator">
                      {sortOrder === 'asc' ? (
                        <ChevronUpIcon className="icon-xs" />
                      ) : (
                        <ChevronDownIcon className="icon-xs" />
                      )}
                    </span>
                  )}
                </div>
              </th>
            ))}
            {actions && <th className="data-table-actions-header">Actions</th>}
          </tr>
        </thead>
        <tbody>
          {data.map((item) => {
            const key = keyExtractor(item);
            const isExpanded = expandable?.isExpanded(item);
            
            return (
              <>
                <tr
                  key={key}
                  className={`data-table-row ${onRowClick ? 'clickable' : ''} ${isExpanded ? 'expanded' : ''}`}
                  onClick={() => onRowClick?.(item)}
                >
                  {selectable && (
                    <td className="data-table-checkbox-cell" onClick={(e) => e.stopPropagation()}>
                      <input
                        type="checkbox"
                        checked={selectable.selected.has(key)}
                        onChange={() => selectable.onSelect(key)}
                      />
                    </td>
                  )}
                  {expandable && (
                    <td className="data-table-expand-cell" onClick={(e) => {
                      e.stopPropagation();
                      expandable.onToggle(item);
                    }}>
                      <button className="expand-button">
                        <ChevronDownIcon className={`icon-sm ${isExpanded ? 'rotated' : ''}`} />
                      </button>
                    </td>
                  )}
                  {columns.map((column) => (
                    <td
                      key={column.key}
                      className="data-table-cell"
                      style={{ textAlign: column.align || 'left' }}
                    >
                      {column.accessor ? column.accessor(item) : (item as any)[column.key]}
                    </td>
                  ))}
                  {actions && (
                    <td className="data-table-actions-cell" onClick={(e) => e.stopPropagation()}>
                      {actions(item)}
                    </td>
                  )}
                </tr>
                {expandable && isExpanded && (
                  <tr key={`${key}-expanded`} className="data-table-expanded-row">
                    <td colSpan={columns.length + (selectable ? 1 : 0) + (actions ? 1 : 0) + 1}>
                      <div className="data-table-expanded-content">
                        {expandable.renderExpanded(item)}
                      </div>
                    </td>
                  </tr>
                )}
              </>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

export default DataTable;