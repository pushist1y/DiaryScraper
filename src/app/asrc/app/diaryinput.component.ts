import { Component, OnInit } from '@angular/core';
import {FormControl, FormGroupDirective, NgForm, Validators} from '@angular/forms';
import {ErrorStateMatcher} from '@angular/material/core';

@Component({
  selector: 'diary-input',
  styleUrls : [
    'diaryinput.component.css'
  ],
  templateUrl: './diaryinput.component.html'
})
export class DiaryInputComponent implements OnInit {

  constructor() { }


  ngOnInit() {  }

  diaryNameFormControl = new FormControl('', [
    Validators.required,
    Validators.pattern(/^[\w-]+$/)
  ]);

  instantErrorStateMatcher = new MyErrorStateMatcher();

  onClickOk(){
    alert(JSON.stringify(this.diaryNameFormControl.errors));
  }
}
      
export class MyErrorStateMatcher implements ErrorStateMatcher {
  isErrorState(control: FormControl | null, form: FormGroupDirective | NgForm | null): boolean {
    const isSubmitted = form && form.submitted;
    let errorState = !!(control && control.invalid && (control.dirty || control.touched || isSubmitted));
    return errorState;
  }
}