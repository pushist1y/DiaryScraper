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
  MatProgressBarModule,
  MatMenuModule

} from '@angular/material';
import { AppRoutingModule } from './app-routing.module';
import { DiaryProgressComponent } from './diary-progress.component';
import { DataService } from '../services/data.service';
import { ScrapeTaskService } from '../services/scrape-task-service';
import { HttpClientModule } from '@angular/common/http';
import { AppStateService } from '../services/appstate.service';
import { DiaryParseInputComponent } from './diary-parse-input.component';
import { ParseInputDataService } from '../services/parse-input-service';
import { ValuesPipe } from '../services/values-pipe';
import { DiaryParseProgressComponent } from './diary-parse-progress.component';
import { ParseTaskService } from '../services/parse-task-service';



@NgModule({
  declarations: [
    AppComponent,
    DiaryInputComponent,
    MomentValidateExactDirective,
    DiaryProgressComponent,
    DiaryParseInputComponent,
    ValuesPipe,
    DiaryParseProgressComponent
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
    MatProgressBarModule,
    MatMenuModule
  ],
  providers: [
    DataService,
    ScrapeTaskService,
    AppStateService,
    ParseInputDataService,
    ParseTaskService
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
