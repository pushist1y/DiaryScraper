import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { NgModule } from '@angular/core';
import { FlexLayoutModule } from "@angular/flex-layout";
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { MatMomentDateModule } from '@angular/material-moment-adapter';
import { MomentValidateExactDirective } from '../directives/moment-validate-exact.directive';

import { AppComponent } from './app.component';
import { DiaryInputComponent } from './diaryinput.component';

import {
  MatFormFieldModule,
  MatOptionModule,
  MatInputModule,
  MatSelectModule,
  MatButtonModule,
  MatIconModule,
  MatCheckboxModule,
  MatDatepickerModule,
  MatProgressBarModule

} from '@angular/material';
import { AppRoutingModule } from './app-routing.module';
import { DiaryProgressComponent } from './diary-progress.component';
import { DataService } from '../services/data.service';
import { ScrapeTaskService } from '../services/scrape-task-service';
import { HttpClientModule } from '@angular/common/http';



@NgModule({
  declarations: [
    AppComponent,
    DiaryInputComponent,
    MomentValidateExactDirective,
    DiaryProgressComponent
  ],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    FlexLayoutModule,
    FormsModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatOptionModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatCheckboxModule,
    MatDatepickerModule,
    MatMomentDateModule,
    AppRoutingModule,
    HttpClientModule,
    MatProgressBarModule
  ],
  providers: [DataService, ScrapeTaskService],
  bootstrap: [AppComponent]
})
export class AppModule { }
