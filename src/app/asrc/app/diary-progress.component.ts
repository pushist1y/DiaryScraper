import { Component, OnInit, HostBinding } from '@angular/core';
import { slideInDownAnimation } from "./animations"
import { DataService } from '../services/data.service';
import { DiaryScraperInputData } from '../common/diary-scraper-input-data';
import { Router, NavigationEnd } from '@angular/router';
import { ScrapeTaskService } from '../services/scrape-task-service';
import { ScrapeTaskDescriptor } from '../common/scrape-task-descriptor';

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
    private scrapeService: ScrapeTaskService) {

  }

  private currentTask: ScrapeTaskDescriptor;
  startWork() {
    this.currentTask = null;

    let newTask = new ScrapeTaskDescriptor();
    newTask.diaryUrl = `http://${this.inputData.diaryAddress}.diary.ru`;
    newTask.workingDir = this.inputData.workingDir;
    newTask.overwrite = this.inputData.overwrite;
    if (this.inputData.dateStart.enabled) {
      newTask.scrapeStart = this.inputData.dateStart.value;
    }
    if (this.inputData.dateEnd.enabled) {
      newTask.scrapeEnd = this.inputData.dateEnd.value;
    }

    // this.scrapeService.startScraping(newTask, this.inputData.diaryLogin, this.inputData.diaryPassword).subscribe(returnedTask => {
    //   this.currentTask = returnedTask;
    // });

  }

  onResetClick() {
    this.router.navigateByUrl("/input");
  }

  ngOnInit() {

    this.dataService.currentData.subscribe(inputData => this.inputData = inputData);
    this.startWork();
    // this.router.events.subscribe(event => {
    //   if (event instanceof NavigationEnd) {
    //     let typedEvent = event as NavigationEnd;
    //     if (!typedEvent.url.includes("progress")) {
    //       return;
    //     }
    //     this.startWork();
    //   }
    // });

  }

}
