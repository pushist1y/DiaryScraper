import { Component, OnInit, HostBinding } from '@angular/core';
import { slideInDownAnimation } from './animations';
import { AppStateService } from '../services/appstate.service';
import { Router } from '@angular/router';
import { Observable, Subscription } from 'rxjs/Rx';
import { ApplicationState } from '../common/app-state';
import { ParseTaskDescriptor, ArchiveTaskDescriptor } from '../common/scrape-task-descriptor';
import { ParseTaskService } from '../services/parse-task-service';
import { ParseInputDataService } from '../services/parse-input-service';
import { DiaryParserInputData } from '../common/diary-parser-input-data';
import { HttpErrorResponse } from '@angular/common/http';
import { ProgressComponentBase } from './progress-component-base';
import { IRemoteProcessService, IRemoteProcessSericeStartArgs } from '../services/remote-service-interface';
import { ArchiveTaskService } from '../services/archive-task-service';
import { ArchiveInputDataService } from '../services/archive-input-service';
import { DiaryArchiverInputData } from '../common/diary-archiver-input-data';

@Component({
  selector: 'app-diary-archive-progress',
  templateUrl: './progress-component-base.html',
  styleUrls: ['./progress-component-base.css'],
  animations: [slideInDownAnimation]
})
export class DiaryArchiveProgressComponent extends ProgressComponentBase implements OnInit {
  menuEnabled: boolean = false;
  title: string = "Архивирование данных";

  getServiceStartArgs(): IRemoteProcessSericeStartArgs {
    let newTask = new ArchiveTaskDescriptor();
    newTask.workingDir = this.archiveInputData.diaryDir;
    return {
      descriptor: newTask
    } as IRemoteProcessSericeStartArgs;
  }


  getService(): IRemoteProcessService {
    return this.archiveTaskService;
  }
  constructor(router: Router,
    appStateService: AppStateService,
    private archiveTaskService: ArchiveTaskService,
    private archiveInputDataService: ArchiveInputDataService) {
    super(router, appStateService)
  }

  archiveInputData: DiaryArchiverInputData;

  ngOnInit() {

    var sub = this.archiveInputDataService.currentData.subscribe(newData => this.archiveInputData = newData);
    this.subscriptions.push(sub);

    super.ngOnInit();

  }

  onResetClick() {
    this.router.navigateByUrl("/archive");
  }

}

