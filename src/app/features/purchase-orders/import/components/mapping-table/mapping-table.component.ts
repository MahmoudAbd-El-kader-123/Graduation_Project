import { Component, EventEmitter, Input, Output, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { SelectModule } from 'primeng/select';
import { SystemField } from '../../../../../shared/constants/import-system-fields';
import { ColumnMapping } from '../../../models/vendor-mapping.model';

@Component({
  selector: 'app-mapping-table',
  standalone: true,
  imports: [CommonModule, FormsModule, TableModule, SelectModule],
  templateUrl: './mapping-table.component.html'
})
export class MappingTableComponent implements OnInit {
  @Input() systemFields: SystemField[] = [];
  @Input() excelHeaders: string[] = [];
  @Input() mappings: ColumnMapping[] = [];
  @Input() readonly: boolean = false;

  @Output() mappingsChange = new EventEmitter<ColumnMapping[]>();

  headerOptions: { label: string, value: string, disabled?: boolean }[] = [];
  
  // Local state to bind UI to avoid reference issues
  localMappings: { [systemField: string]: string | null } = {};
  
  // Options specific to each field to handle disabled states
  optionsPerField: { [fieldId: string]: { label: string, value: string, disabled?: boolean }[] } = {};

  ngOnInit() {
    this.headerOptions = [
      ...this.excelHeaders.map(h => ({ label: h, value: h }))
    ];

    // Initialize local state from input, defaulting to null for unmapped fields
    this.systemFields.forEach(f => {
      const mapping = this.mappings.find(m => m.systemField === f.id);
      this.localMappings[f.id] = mapping ? mapping.excelColumn : null;
    });

    this.updateOptionsPerField();
  }

  updateOptionsPerField() {
    this.systemFields.forEach(field => {
      this.optionsPerField[field.id] = this.headerOptions.map(opt => {
        const isMappedElsewhere = Object.entries(this.localMappings).some(
          ([key, value]) => key !== field.id && value === opt.value && value !== null
        );
        return {
          ...opt,
          disabled: isMappedElsewhere
        };
      });
    });
  }

  onMappingChange(systemField: string, excelColumn: string | null) {
    if (this.readonly) return;
    
    this.localMappings[systemField] = excelColumn;
    this.updateOptionsPerField();
    this.emitMappings();
  }

  private emitMappings() {
    const updatedMappings: ColumnMapping[] = Object.keys(this.localMappings)
      .filter(field => this.localMappings[field] !== null && this.localMappings[field] !== undefined) // only include mapped fields
      .map(field => ({
        systemField: field,
        excelColumn: this.localMappings[field] as string,
        isCustom: false
      }));

    this.mappingsChange.emit(updatedMappings);
  }

  getMappedHeader(systemField: string): string {
    return this.localMappings[systemField] || 'Unmapped';
  }
}
