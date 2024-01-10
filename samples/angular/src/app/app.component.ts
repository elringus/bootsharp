import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import backend from "backend";
import { Computer } from "backend";

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {
  async ngOnInit() {
    await backend.boot({ root: "/assets/bin" });

    Computer.onComplete.subscribe(console.log);

    // if (Computer.isComputing()) Computer.stopComputing();
    // else 
    Computer.startComputing();



  }
  title = 'angular';
}
