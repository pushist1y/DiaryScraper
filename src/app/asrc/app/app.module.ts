import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FlexLayoutModule } from "@angular/flex-layout";
import { FormsModule } from '@angular/forms';


import { AppComponent } from './app.component';
import { DiaryComponent } from './diary.component';


@NgModule({
  declarations: [
    AppComponent,
    DiaryComponent
  ],
  imports: [
    BrowserModule,
    FlexLayoutModule,
    FormsModule
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
