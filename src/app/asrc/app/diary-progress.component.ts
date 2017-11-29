import { Component, OnInit, HostBinding, Pipe, PipeTransform } from '@angular/core';
import { slideInDownAnimation } from "./animations"
import { DataService } from '../services/data.service';
import { DiaryScraperInputData } from '../common/diary-scraper-input-data';
import { Router, NavigationEnd } from '@angular/router';
import { ScrapeTaskService } from '../services/scrape-task-service';
import { ScrapeTaskDescriptor } from '../common/scrape-task-descriptor';
import { Observable, Subscription } from 'rxjs/Rx';
import { HttpErrorResponse } from '@angular/common/http';
import * as moment from 'moment';
import { ApplicationState } from '../common/app-state';
import { AppStateService } from '../services/appstate.service';

@Component({
  selector: 'app-diary-progress',
  templateUrl: './diary-progress.component.html',
  styleUrls: ['./diary-progress.component.css'],
  animations: [slideInDownAnimation]

})
export class DiaryProgressComponent implements OnInit {
  @HostBinding('@routeAnimation') routeAnimation = true;
  @HostBinding('style.display') display = 'block';
  // @HostBinding('style.position') position = 'absolute';


  private inputData: DiaryScraperInputData;
  constructor(private dataService: DataService,
    private router: Router,
    private appStateService: AppStateService,
    private scrapeService: ScrapeTaskService) {
    this.progressModel = new ProgressModel();
  }

  progressModel: ProgressModel;

  startWork() {
    this.progressModel.currentTask = null;

    let newTask = new ScrapeTaskDescriptor();
    newTask.diaryUrl = `http://${this.inputData.diaryAddress}.diary.ru`;
    newTask.workingDir = this.inputData.workingDir;
    newTask.overwrite = this.inputData.overwrite;
    newTask.requestDelay = this.inputData.requestDelay;
    if (this.inputData.dateStart.enabled) {
      newTask.scrapeStart = moment(this.inputData.dateStart.value).utc().subtract(new Date().getTimezoneOffset(), 'm');
    }
    if (this.inputData.dateEnd.enabled) {
      newTask.scrapeEnd = moment(this.inputData.dateEnd.value).utc().subtract(new Date().getTimezoneOffset(), 'm')
    }

    this.scrapeService
      .startScraping(newTask, this.inputData.diaryLogin, this.inputData.diaryPassword)
      .subscribe(returnedTask => {
        console.log("starting scraping");
        this.updateTaskData(returnedTask);
        this.progressModel.inProgress = true;
        this.progressModel.scheduler = Observable.interval(1000);
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
    this.scrapeService
      .updateScraping(this.progressModel.currentTask.guidString)
      .subscribe((updatedTask: ScrapeTaskDescriptor) => {
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

  updateTaskData(newTask: ScrapeTaskDescriptor) {
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
    this.scrapeService.cancelScraping(this.progressModel.currentTask.guidString)
      .subscribe((cancelledTask: ScrapeTaskDescriptor) => {
        console.log("cancel finished");
        this.updateTaskData(cancelledTask);
        this.progressModel.isCancelling = false;
      }, (error: HttpErrorResponse) => {
        this.progressModel.isCancelling = false;
        this.progressModel.currentTask.error = error.message;
      })
  }

  onResetClick() {
    this.router.navigateByUrl("/input");
  }

  onCancelClick() {
    console.log("cancel clicked");
    this.cancelTask();
  }
 
  private subscriptions: Array<Subscription> = new Array<Subscription>();
  private appState: ApplicationState = new ApplicationState();
  
  ngOnInit() {
    var sub = this.dataService.currentData.subscribe(inputaData => this.inputData = inputaData);
    this.subscriptions.push(sub);

    sub = this.appStateService.currentState.subscribe(newState => this.appState = newState);
    this.subscriptions.push(sub);

    this.appState.menuEnabled = false;
    this.appState.title = 'Выгрузка дневников';
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

}




export class ProgressModel {
  currentTask: ScrapeTaskDescriptor;
  progressValue: number = 0;
  inProgress: boolean = false;
  scheduler: Observable<number>;
  subscription: Subscription;
  isCancelling: boolean = false;;
}