import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { NgModule } from '@angular/core';
import { FlexLayoutModule } from "@angular/flex-layout";
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { MatMomentDateModule } from '@angular/material-moment-adapter';
import { MomentValidateExactDirective } from '../directives/moment-validate-exact.directive';

import { AppComponent, DialogOverviewExampleDialog } from './app.component';
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
  MatMenuModule,
  MatDialogModule

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
import { DiaryArchiveInputComponent } from './diary-archive-input.component';
import { ArchiveTaskService } from '../services/archive-task-service';
import { ArchiveInputDataService } from '../services/archive-input-service';
import { DiaryArchiveProgressComponent } from './diary-archive-progress.component';



@NgModule({
  declarations: [
    AppComponent,
    DiaryInputComponent,
    MomentValidateExactDirective,
    DiaryProgressComponent,
    DiaryParseInputComponent,
    ValuesPipe,
    DiaryParseProgressComponent,
    DiaryArchiveInputComponent,
    DiaryArchiveProgressComponent,
    DialogOverviewExampleDialog
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
    MatMenuModule,
    MatDialogModule
  ],
  providers: [
    DataService,
    ScrapeTaskService,
    AppStateService,
    ParseInputDataService,
    ParseTaskService,
    ArchiveTaskService,
    ArchiveInputDataService
  ],
  entryComponents: [DialogOverviewExampleDialog],
  bootstrap: [AppComponent]
})
export class AppModule { }
