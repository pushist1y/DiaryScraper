/// <reference path="../../node_modules/electron/electron.d.ts"/>
//c:\work\DiaryScraper\src\app\node_modules\@types\node\index.d.ts 

import { Component, OnInit, ViewChild, Directive, Input, HostBinding } from '@angular/core';
import { FormControl, FormGroupDirective, NgForm, Validators, ValidatorFn, Validator, AbstractControl, NgModel, NG_VALIDATORS } from '@angular/forms';
import { ErrorStateMatcher } from '@angular/material/core';
import { Location } from '@angular/common';
import { DateAdapter, MAT_DATE_FORMATS } from '@angular/material/core';
import { Router } from '@angular/router';
import { slideInDownAnimation } from './animations';
import { DiaryScraperInputData } from '../common/diary-scraper-input-data';
import { DataService } from '../services/data.service';

declare var electron: Electron.AllElectron;

const currentWindow = electron.remote.getCurrentWindow();
const dialog = electron.remote.dialog;

export const RU_FORMATS = {
  parse: {
    dateInput: 'DD.MM.YYYY',
  },
  display: {
    dateInput: 'DD.MM.YYYY',
    monthYearLabel: 'MMM YYYY',
    dateA11yLabel: 'DD.MM.YYYY',
    monthYearA11yLabel: 'MMMM YYYY',
  },
};

@Component({
  selector: 'diary-input',
  styleUrls: [
    'diaryinput.component.css'
  ],
  templateUrl: './diaryinput.component.html',
  providers: [
    { provide: MAT_DATE_FORMATS, useValue: RU_FORMATS }
  ],
  animations: [slideInDownAnimation]
})
export class DiaryInputComponent implements OnInit {

  constructor(private adapter: DateAdapter<any>, private location: Location,
    private router: Router, private dataService: DataService) {
    this.adapter.setLocale("ru");
  }

  @HostBinding('@routeAnimation') routeAnimation = true;
  @HostBinding('style.display') display = 'block';
  // @HostBinding('style.position') position = 'absolute';

  ngOnInit() {
    this.dataService.currentData.subscribe(inputaData => this.inputData = inputaData);
  }

  inputData: DiaryScraperInputData;

  instantErrorStateMatcher = new MyErrorStateMatcher();

  onWorkingDirectoryButtonClick(workingDirFormControl: FormControl) {
    let paths = dialog.showOpenDialog(currentWindow, {
      properties: ['openDirectory']
    });

    if (paths && paths.length > 0) {
      this.inputData.workingDir = paths[0];
    }
  }

  onSubmit() {
    this.dataService.changeData(this.inputData);
    this.router.navigateByUrl("/progress");
  }
}

export class MyErrorStateMatcher implements ErrorStateMatcher {
  isErrorState(control: FormControl | null, form: FormGroupDirective | NgForm | null): boolean {
    const isSubmitted = form && form.submitted;
    let errorState = !!(control && control.invalid && (control.dirty || control.touched || isSubmitted));
    return errorState;
  }
}





