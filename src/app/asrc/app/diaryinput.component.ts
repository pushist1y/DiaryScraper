/// <reference path="../../node_modules/electron/electron.d.ts"/>
//c:\work\DiaryScraper\src\app\node_modules\@types\node\index.d.ts 

import { Component, OnInit, ViewChild } from '@angular/core';
import { FormControl, FormGroupDirective, NgForm, Validators } from '@angular/forms';
import { ErrorStateMatcher } from '@angular/material/core';

declare var electron: Electron.AllElectron;

const currentWindow = electron.remote.getCurrentWindow();
const dialog = electron.remote.dialog;

@Component({
  selector: 'diary-input',
  styleUrls: [
    'diaryinput.component.css'
  ],
  templateUrl: './diaryinput.component.html'
})
export class DiaryInputComponent implements OnInit {

  constructor() { }

  @ViewChild('#diaryNameInput')
  diaryNameInput: any;

  ngOnInit() { }

  inputData = new DiaryScraperInputData();



  instantErrorStateMatcher = new MyErrorStateMatcher();

  onClickOk(diaryNameInput: FormControl) {

    alert(JSON.stringify(diaryNameInput.errors));
  }

  onWorkingDirectoryButtonClick(workingDirFormControl: FormControl) {
    let paths = dialog.showOpenDialog(currentWindow, {
      properties: ['openDirectory']
    });
    
    if(paths && paths.length > 0)
    {
      this.inputData.workingDir = paths[0];
    }
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
}