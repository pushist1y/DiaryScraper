import { Observable, Subscription } from 'rxjs/Rx';
import * as moment from 'moment';
import { IRemoteProcessSericeStartArgs, IRemoteProcessService } from '../services/remote-service-interface';
import { Component, OnInit } from '@angular/core';
import { slideInDownAnimation } from './animations';
import { ProgressComponentBase } from './progress-component-base';
import { ScrapeTaskDescriptor } from '../common/scrape-task-descriptor';
import { IRemoteProcessScrapingSericeStartArgs, ScrapeTaskService } from '../services/scrape-task-service';
import { DiaryScraperInputData } from '../common/diary-scraper-input-data';
import { DataService } from '../services/data.service';
import { Router } from '@angular/router';
import { AppStateService } from '../services/appstate.service';


@Component({
  selector: 'app-diary-progress',
  templateUrl: './progress-component-base.html',
  styleUrls: ['./progress-component-base.css'],
  animations: [slideInDownAnimation]

})
export class DiaryProgressComponent extends ProgressComponentBase implements OnInit {
  title: string = 'Выгрузка дневников';

  getServiceStartArgs(): IRemoteProcessSericeStartArgs {
    let newTask = new ScrapeTaskDescriptor();
    newTask.diaryUrl = `http://${this.inputData.diaryAddress}.diary.ru`;
    newTask.workingDir = this.inputData.workingDir;
    newTask.overwrite = this.inputData.overwrite;
    newTask.downloadEdits = this.inputData.downloadEdits;
    newTask.downloadAccount = this.inputData.downloadAccount;
    newTask.requestDelay = this.inputData.requestDelay;
    if (this.inputData.dateStart.enabled) {
      newTask.scrapeStart = moment(this.inputData.dateStart.value).utc().subtract(new Date().getTimezoneOffset(), 'm');
    }
    if (this.inputData.dateEnd.enabled) {
      newTask.scrapeEnd = moment(this.inputData.dateEnd.value).utc().subtract(new Date().getTimezoneOffset(), 'm')
    }

    let args = {
      descriptor: newTask,
      login: this.inputData.diaryLogin,
      password: this.inputData.diaryPassword
    } as IRemoteProcessScrapingSericeStartArgs

    return args;
  }

  getService(): IRemoteProcessService {
    return this.scrapeService;
  }

  private inputData: DiaryScraperInputData;
  constructor(private dataService: DataService,
    router: Router,
    appStateService: AppStateService,
    private scrapeService: ScrapeTaskService) {

    super(router, appStateService);
  }

  onResetClick() {
    this.router.navigateByUrl("/input");
  }

  ngOnInit() {
    var sub = this.dataService.currentData.subscribe(inputaData => this.inputData = inputaData);
    this.subscriptions.push(sub);
    super.ngOnInit();
  }

}

