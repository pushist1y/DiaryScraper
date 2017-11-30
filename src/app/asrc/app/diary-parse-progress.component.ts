import { Component, OnInit, HostBinding } from '@angular/core';
import { slideInDownAnimation } from './animations';
import { AppStateService } from '../services/appstate.service';
import { Router } from '@angular/router';
import { Observable, Subscription } from 'rxjs/Rx';
import { ApplicationState } from '../common/app-state';
import { ProgressModelBase } from './diary-progress.component';
import { ParseTaskDescriptor } from '../common/scrape-task-descriptor';
import { ParseTaskService } from '../services/parse-task-service';
import { ParseInputDataService } from '../services/parse-input-service';
import { DiaryParserInputData } from '../common/diary-parser-input-data';
import { HttpErrorResponse } from '@angular/common/http';

@Component({
  selector: 'app-diary-parse-progress',
  templateUrl: './diary-parse-progress.component.html',
  styleUrls: ['./diary-parse-progress.component.css'],
  animations: [slideInDownAnimation]
})
export class DiaryParseProgressComponent implements OnInit {
  @HostBinding('@routeAnimation') routeAnimation = true;
  @HostBinding('style.display') display = 'block';
  constructor(private router: Router,
    private appStateService: AppStateService,
    private parseService: ParseTaskService,
    private parseInputService: ParseInputDataService) { }

  private subscriptions: Array<Subscription> = new Array<Subscription>();
  private appState: ApplicationState = new ApplicationState();
  private progressModel: ProgressModel = new ProgressModel();
  private parseInputData: DiaryParserInputData;

  ngOnInit() {

    var sub = this.parseInputService.currentData.subscribe(newParseData => this.parseInputData = newParseData);
    this.subscriptions.push(sub);

    sub = this.appStateService.currentState.subscribe(newState => this.appState = newState);
    this.subscriptions.push(sub);

    this.appState.menuEnabled = false;
    this.appState.title = 'Обработка данных';
    this.appStateService.changeState(this.appState);

    this.startWork();
  }

  ngOnDestroy() {
    if (!this.subscriptions) {
      return;
    }
    this.subscriptions.forEach((sub) => {
      sub.unsubscribe();
    })
  }

  startWork() {
    this.progressModel.currentTask = null;

    let newTask = new ParseTaskDescriptor();
    newTask.workingDir = this.parseInputData.diaryDir;


    this.parseService
      .startParsing(newTask)
      .subscribe(returnedTask => {
        console.log("starting parsing");
        this.updateTaskData(returnedTask);
        this.progressModel.inProgress = true;
        this.progressModel.scheduler = Observable.interval(333);
        this.progressModel.subscription = this.progressModel.scheduler.subscribe((value: number) => {
          console.log("calling refresh");
          this.refreshTask();
        });
      }, (error: HttpErrorResponse) => {
        this.stopProgress();
        this.progressModel.currentTask = newTask;
        this.progressModel.currentTask.error = error.message;
      });

  }

  refreshTask() {
    if (!this.progressModel.inProgress || this.progressModel.currentTask === undefined) {
      return;
    }
    console.log("refreshing");
    this.parseService
      .updateParsing(this.progressModel.currentTask.guidString)
      .subscribe((updatedTask: ParseTaskDescriptor) => {
        console.log("refresh finished");
        this.updateTaskData(updatedTask);
        if ((updatedTask.status && updatedTask.status >= 5) || !!updatedTask.error) {
          this.stopProgress();

        }
      }, (error: HttpErrorResponse) => {
        this.stopProgress();
        this.progressModel.currentTask.error = error.message;
      });
  }

  updateTaskData(newTask: ParseTaskDescriptor) {
    this.progressModel.currentTask = newTask;
  }

  stopProgress() {
    console.log("stopping progress");
    if (this.progressModel.subscription) {
      this.progressModel.subscription.unsubscribe();
      this.progressModel.subscription = null;
    }
    this.progressModel.scheduler = null;
    this.progressModel.inProgress = false;
  }

  cancelTask() {
    if (!this.progressModel.inProgress || this.progressModel.currentTask === undefined) {
      return;
    }
    this.progressModel.isCancelling = true;
    this.stopProgress();
    console.log("cancelling");
    this.parseService.cancelParsing(this.progressModel.currentTask.guidString)
      .subscribe((cancelledTask: ParseTaskDescriptor) => {
        console.log("cancel finished");
        this.updateTaskData(cancelledTask);
        this.progressModel.isCancelling = false;
      }, (error: HttpErrorResponse) => {
        this.progressModel.isCancelling = false;
        this.progressModel.currentTask.error = error.message;
      })
  }

  onResetClick() {
    this.router.navigateByUrl("/parse");
  }

  onCancelClick() {
    console.log("cancel clicked");
    this.cancelTask();
  }

}

class ProgressModel extends ProgressModelBase {
  currentTask: ParseTaskDescriptor;
}