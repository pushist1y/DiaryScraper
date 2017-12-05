import { Component, OnInit, HostBinding } from '@angular/core';
import { slideInDownAnimation } from './animations';
import { AppStateService } from '../services/appstate.service';
import { Router } from '@angular/router';
import { Observable, Subscription } from 'rxjs/Rx';
import { ApplicationState } from '../common/app-state';
import { ParseTaskDescriptor } from '../common/scrape-task-descriptor';
import { ParseTaskService } from '../services/parse-task-service';
import { ParseInputDataService } from '../services/parse-input-service';
import { DiaryParserInputData } from '../common/diary-parser-input-data';
import { HttpErrorResponse } from '@angular/common/http';
import { ProgressComponentBase } from './progress-component-base';
import { IRemoteProcessService, IRemoteProcessSericeStartArgs } from '../services/remote-service-interface';

@Component({
  selector: 'app-diary-parse-progress',
  templateUrl: './progress-component-base.html',
  styleUrls: ['./progress-component-base.css'],
  animations: [slideInDownAnimation]
})
export class DiaryParseProgressComponent extends ProgressComponentBase implements OnInit {
  title: string = "Обработка данных";

  getServiceStartArgs(): IRemoteProcessSericeStartArgs {
    let newTask = new ParseTaskDescriptor();
    newTask.workingDir = this.parseInputData.diaryDir;
    return {
      descriptor: newTask
    } as IRemoteProcessSericeStartArgs;
  }


  getService(): IRemoteProcessService {
    return this.parseService;
  }
  constructor(router: Router,
    appStateService: AppStateService,
    private parseService: ParseTaskService,
    private parseInputService: ParseInputDataService) {
    super(router, appStateService)
  }

  parseInputData: DiaryParserInputData;

  ngOnInit() {

    var sub = this.parseInputService.currentData.subscribe(newParseData => this.parseInputData = newParseData);
    this.subscriptions.push(sub);

    super.ngOnInit();

  }

  onResetClick() {
    this.router.navigateByUrl("/parse");
  }

}

