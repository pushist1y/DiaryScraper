/// <reference path="../../node_modules/electron/electron.d.ts"/>
//c:\work\DiaryScraper\src\app\node_modules\@types\node\index.d.ts 

import { Component, OnInit, ViewChild, Directive, Input, HostBinding } from '@angular/core';
import { FormControl, FormGroupDirective, NgForm, Validators, ValidatorFn, Validator, AbstractControl, NgModel, NG_VALIDATORS } from '@angular/forms';
import { ErrorStateMatcher } from '@angular/material/core';
import { Location } from '@angular/common';
import * as moment from 'moment';
import { DateAdapter, MAT_DATE_FORMATS } from '@angular/material/core';
import { Router } from '@angular/router';
import { slideInDownAnimation } from './animations';

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

  constructor(private adapter: DateAdapter<any>, private location: Location, private router: Router) {
    this.adapter.setLocale("ru");
  }

  @HostBinding('@routeAnimation') routeAnimation = true;
  @HostBinding('style.display')   display = 'block';
  @HostBinding('style.position')  position = 'absolute';

  ngOnInit() { }

  inputData = new DiaryScraperInputData();



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
    alert("Submitting");
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

export class DiaryScraperInputData {
  public diaryLogin: string = "";
  public diaryPass: string = "";
  public diaryAddress: string = "";
  public workingDir: string = "";
  public dateStart: DateCheck;
  public dateEnd: DateCheck;
  public overwrite: boolean = false;

  constructor() {
    this.dateStart = new DateCheck();
    this.dateStart.value = moment([2000, 0, 1]);
    this.dateEnd = new DateCheck();
    this.dateEnd.value = moment([2025, 0, 1]);
  }
}

export class DateCheck {
  public value: moment.Moment = moment();
  public enabled: boolean = false;
}



@Directive({
  selector: '[appForbiddenName]',
  providers: [{ provide: NG_VALIDATORS, useExisting: ForbiddenValidatorDirective, multi: true }]
})
export class ForbiddenValidatorDirective implements Validator {
  @Input() forbiddenName: string;

  validate(control: AbstractControl): { [key: string]: any } {
    return this.forbiddenName ? forbiddenNameValidator(new RegExp(this.forbiddenName, 'i'))(control)
      : null;
  }
}

/** A hero's name can't match the given regular expression */
export function forbiddenNameValidator(nameRe: RegExp): ValidatorFn {
  return (control: AbstractControl): { [key: string]: any } => {
    const forbidden = nameRe.test(control.value);
    return forbidden ? { 'forbiddenName': { value: control.value } } : null;
  };
}