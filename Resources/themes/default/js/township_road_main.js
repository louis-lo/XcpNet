$(document).ready(function(){
     //主页轮播
    var mySwiper = new Swiper('.swiper-container',{
    pagination: '.pagination',
    paginationClickable: true,
    moveStartThreshold: 100,
    autoplay:2000,
    loop:true,
    speed:600,
  }) ;
    $('.arrow-left').bind('click', function(e){
        e.preventDefault()
        mySwiper.swipePrev()
      })
      $('.arrow-right').bind('click', function(e){
        e.preventDefault()
        mySwiper.swipeNext()
      });
    $('.arrow-left').css("display","none");
    $('.arrow-right').css("display","none");
    //轮播鼠标悬停
    $(".swiper-container").hover(
    	function(){
    		mySwiper.stopAutoplay();
    		  $('.arrow-right').css({"display":"block","color":"#fff"});
    		  $('.arrow-left').css({"display":"block","color":"#fff"});
    		
    	},function(){
    		mySwiper.startAutoplay();
    		 $('.arrow-left').css("display","none");
    		 $('.arrow-right').css("display","none");
    	}
    );
});


