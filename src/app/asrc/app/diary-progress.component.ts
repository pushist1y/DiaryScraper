import { Component, OnInit, HostBinding } from '@angular/core';
 import { slideInDownAnimation } from "./animations"

@Component({
  selector: 'app-diary-progress',
  templateUrl: './diary-progress.component.html',
  styleUrls: ['./diary-progress.component.css'],
  animations: [slideInDownAnimation]
})
export class DiaryProgressComponent implements OnInit {
  @HostBinding('@routeAnimation') routeAnimation = true;
  @HostBinding('style.display')   display = 'block';
  @HostBinding('style.position')  position = 'absolute';

  constructor() { }

  ngOnInit() {
  }

}
